terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=3.74.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "=3.1.0"
    }
  }
  backend "azurerm" {

  }
}

provider "azurerm" {
  features {}

  subscription_id = var.subscription_id
}

locals {
  func_name = "qbavail${random_string.unique.result}"
  gh_repo = replace(var.gh_repo, "implodingduck/", "")
  tags = {
    "managed_by" = "terraform"
    "repo"       = local.gh_repo
  }
}

data "azurerm_client_config" "current" {}


resource "azurerm_resource_group" "rg" {
  name     = "rg-quackbank-trackavailability"
  location = var.location
  tags = local.tags
}

resource "random_string" "unique" {
  length  = 8
  special = false
  upper   = false
}

data "azurerm_application_insights" "appinsights" {
  name                = var.app_insights_name
  resource_group_name = var.app_insights_rg
}

module "func" {
  source = "github.com/implodingduck/tfmodules//functionapp"
  func_name = "${local.func_name}"
  resource_group_name = azurerm_resource_group.rg.name
  resource_group_location = azurerm_resource_group.rg.location
  working_dir = "qbtrackavailability"
  publish = 0
  asp_kind = "Linux"
  plan_tier = "Basic"
  plan_size = "B1"

  linux_fx_version = "DOCKER|ghcr.io/implodingduck/quackers-bank-trackavailability:${var.image_version}"
  app_settings = {
    "FUNCTIONS_WORKER_RUNTIME" = "dotnet"
    "WEBSITES_ENABLE_APP_SERVICE_STORAGE" = "false"
    "AVAILABILITY_APPINSIGHTS_CONNECTION_STRING" = data.azurerm_application_insights.appinsights.connection_string
    "REGION_NAME" = azurerm_resource_group.rg.location
    #"DOCKER_REGISTRY_SERVER_URL" = azurerm_container_registry.test.login_server
    #"DOCKER_REGISTRY_SERVER_USERNAME" = azurerm_container_registry.test.admin_username
    #"DOCKER_REGISTRY_SERVER_PASSWORD" = azurerm_container_registry.test.admin_password
    "BASE_URL" = "@Microsoft.KeyVault(VaultName=${azurerm_key_vault.kv.name};SecretName=BASEURL)"
    "TEST_EMAIL" = "@Microsoft.KeyVault(VaultName=${azurerm_key_vault.kv.name};SecretName=TESTUSER)"
    "TEST_PASSWORD" = "@Microsoft.KeyVault(VaultName=${azurerm_key_vault.kv.name};SecretName=TESTPASSWORD)"
  }
  app_identity = [
      {
          type = "SystemAssigned"
          identity_ids = null
      }
  ]
}

resource "azurerm_container_registry" "test" {
  name                = "acr${local.func_name}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  sku                 = "Standard"
  admin_enabled       = true
}


resource "azurerm_key_vault" "kv" {
  name                       = "kv-${local.func_name}"
  location                   = azurerm_resource_group.rg.location
  resource_group_name        = azurerm_resource_group.rg.name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  soft_delete_retention_days = 7
  purge_protection_enabled = false

  
}

resource "azurerm_key_vault_access_policy" "sp" {
  key_vault_id = azurerm_key_vault.kv.id
  tenant_id = data.azurerm_client_config.current.tenant_id
  object_id = data.azurerm_client_config.current.object_id
  
  key_permissions = [
    "Create",
    "Get",
    "Purge",
    "Recover",
    "Delete"
  ]

  secret_permissions = [
    "Set",
    "Purge",
    "Get",
    "List"
  ]

  certificate_permissions = [
    "Purge"
  ]

  storage_permissions = [
    "Purge"
  ]
  
}


resource "azurerm_key_vault_access_policy" "as" {
  key_vault_id = azurerm_key_vault.kv.id
  tenant_id = data.azurerm_client_config.current.tenant_id
  object_id = module.func.identity_principal_id
  
  key_permissions = [
    "Get",
  ]

  secret_permissions = [
    "Get",
    "List"
  ]
  
}
