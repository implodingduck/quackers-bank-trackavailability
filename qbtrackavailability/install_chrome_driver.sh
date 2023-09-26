#!/bin/bash
curl -sO https://googlechromelabs.github.io/chrome-for-testing/LATEST_RELEASE_STABLE
export CHROME_VERSION=$(cat LATEST_RELEASE_STABLE)
echo $CHROME_VERSION
curl -sO https://edgedl.me.gvt1.com/edgedl/chrome/chrome-for-testing/${CHROME_VERSION}/linux64/chrome-linux64.zip
unzip chrome-linux64.zip
mv chrome-linux64 /usr/bin
apt-get purge --auto-remove -y curl gnupg zip
rm -rf /var/lib/apt/lists/*
curl -sOL https://github.com/SeleniumHQ/selenium/raw/trunk/common/manager/linux/selenium-manager
mv selenium-manager  /azure-functions-host/selenium-manager/linux/selenium-manager