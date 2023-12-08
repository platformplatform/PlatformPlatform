#!/usr/bin/env sh

# Navigate to the script's directory
cd "$(dirname "$0")"

# Publish the CLI application
dotnet publish --configuration Release

# Define the full path to the CLI executable
CLI_PATH="$(pwd)/artifacts/bin/DeveloperCli/release/PlatformPlatform.DeveloperCli"
ALIAS_NAME=${1:-ppcli}

if [ -f ~/.bashrc ]; then
    if ! grep -q "alias ${ALIAS_NAME}=" ~/.bashrc; then
        echo "Installing alias '$ALIAS_NAME' in .bashrc"
        echo "alias $ALIAS_NAME='$CLI_PATH'" >> ~/.bashrc
        echo "Added "$ALIAS_NAME" alias. Please restart you terminal or run 'source ~/.bashrc'"
    else
        echo "Alias '$ALIAS_NAME' already exists in .bashrc"
    fi
fi

if [ -f ~/.zshrc ]; then
    if ! grep -q "alias ${ALIAS_NAME}=" ~/.zshrc; then
        echo "Installing alias '$ALIAS_NAME' in .zshrc"
        echo "alias $ALIAS_NAME='$CLI_PATH'" >> ~/.zshrc
        echo "Added "$ALIAS_NAME" alias. Please restart you terminal or run 'source ~/.zshrc'"
    else
        echo "Alias '$ALIAS_NAME' already exists in .zshrc"
    fi
fi
