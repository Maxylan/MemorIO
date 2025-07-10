#!/bin/bash

PROJECT_PATH="$HOME/memorio"
PROJECT_ORIGIN="git@github.com:Maxylan/MemorIO.git"
PROJECT_BRANCH="master"
DEPLOY_PATH="/var/www/memorio/"

source deploy.env

if [ ! -d "$PROJECT_PATH" ]; then
    git clone "$PROJECT_ORIGIN" "$PROJECT_PATH"
fi

cd "$PROJECT_PATH" || exit

git fetch origin
git checkout "$PROJECT_BRANCH"
git pull --rebase=true origin "$PROJECT_BRANCH"

# Deploy..
