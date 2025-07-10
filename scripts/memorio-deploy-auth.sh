#!/bin/bash

PROJECT_PATH="$HOME/memorio"
PROJECT_ORIGIN="git@github.com:Maxylan/MemorIO.git"
PROJECT_BRANCH="master"
DEPLOY_PATH="/var/www/memorio/"

source deploy.env
source $HOME/.nvm/nvm.sh

# Deploy..
cd "$PROJECT_PATH/projects/memorio-auth" || exit

if [ -f ".nvmrc" ]; then
    nvm use
else
    nvm use v22.16.0
fi

if [ ! -d "$PROJECT_PATH" ]; then
    git clone "$PROJECT_ORIGIN" "$PROJECT_PATH"
fi

git fetch origin
git checkout "$PROJECT_BRANCH"
git pull --rebase=true origin "$PROJECT_BRANCH"

npm i && npm run build

if [ ! -d "./dist/auth" ]; then
    echo -e "\nNo './dist/auth' exists, exiting.\n"
    exit
fi

scp -F ~/.ssh/config ./dist/auth "root@auth.torpssons.se:~/auth"
ssh -F ~/.ssh/config root@auth.torpssons.se "'cp -rpv ~/auth $DEPLOY_PATH'"
