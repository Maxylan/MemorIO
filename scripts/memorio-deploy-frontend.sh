#!/bin/bash

PROJECT_PATH="$HOME/memorio"
PROJECT_ORIGIN="git@github.com:Maxylan/MemorIO.git"
PROJECT_BRANCH="master"
DEPLOY_PATH="/var/www/memorio/"

source deploy.env
source $HOME/.nvm/nvm.sh

if [ ! -d "$PROJECT_PATH" ]; then
    git clone "$PROJECT_ORIGIN" "$PROJECT_PATH"
fi

cd "$PROJECT_PATH" || exit

git fetch origin
git checkout "$PROJECT_BRANCH"
git pull --rebase=true origin "$PROJECT_BRANCH"

# Deploy..
cd ./projects/memorio-frontend || exit

if [ -f ".nvmrc" ]; then
    nvm use
else
    nvm use v22.16.0
fi

npm i && npm run build

if [ ! -d "./dist/frontend" ]; then
    echo -e "\nNo './dist/frontend' exists, exiting.\n"
    exit
fi

cp -rpv ./dist/frontend "$DEPLOY_PATH"
