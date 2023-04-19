# Contributing to PlatformPlatform

Thank you for your interest in contributing to the PlatformPlatform project! We appreciate your effort and contributions that will help improve the project. This document outlines the guidelines for contributing to the project.

We encourage you to reach out before you start working on a contribution, especially for larger changes or new features. We have high standards and would like to ensure that your time is well spent, so please consider creating an issue to discuss the proposed changes before diving into the implementation. For smaller bug fixes and clear objective improvements, this step may not be necessary. We look forward to collaborating with you in a friendly and constructive manner!


## Contributor License Agreement (CLA)

By contributing to this project, you agree to grant PlatformPlatform (the owner of the GitHub organization https://github.com/PlatformPlatform)
all rights to your contributions, ensuring flexibility in managing the project and its codebase. Although the project is fully open-source under the MIT License, your contributions grant us the freedom to make necessary changes in the future, if required.

As a contributor, you are responsible for ensuring that your contributions are free from any rights and that you have the authority to grant these rights to the project. PlatformPlatform cannot be held accountable for any claims or disputes arising from contributions that violate this requirement.


## Setting Up the Development Environment

To set up your development environment, follow the instructions in the `README.md` file located in the root of the repository.


## Contributing Workflow

1. **Fork the Repository**: Navigate to the [PlatformPlatform repository](https://github.com/PlatformPlatform/platformplatform) and click the "Fork" button in the upper right-hand corner. This will create a copy of the repository under your GitHub account.

2. **Clone the Fork**: Open a terminal window and navigate to the directory where you want to clone the forked repository. Run the following command, replacing `your-username` with your GitHub username: `git clone https://github.com/your-username/platformplatform.git`

3. **Add the Upstream Remote**: Navigate to the cloned repository and run the following command to add the original PlatformPlatform repository as a remote named `upstream`: `git remote add upstream https://github.com/PlatformPlatform/platformplatform.git`

4. **Create a Branch**: Create a new branch for your contribution, giving it a descriptive name that reflects the changes you intend to make: `git checkout -b your-branch-name`. Branch names should be lowercase and use a dash to separate words.

5. **Make Changes**: Modify the code, ensuring that your changes follow the project's coding style and conventions. The code style must follow what is configured in .editorconfig. It's strongly recommended to use JetBrains Rider or ReSharper to do a full solution code cleanup before committing.

6. **Commit Your Changes**: Stage and commit your changes with descriptive commit messages. Commit messages should be formulated as a sentence in present tense, but do not include the trailing dot. All commits must be signed with GPG. Please follow the [GitHub guide](https://docs.github.com/en/authentication/managing-commit-signature-verification) on setting up GPG signing.

7. **Pull the Latest Changes from Upstream**: Before pushing your changes to your fork, pull the latest changes from the upstream repository to ensure that your changes are compatible with the current state of the project: `git pull upstream main`

8. **Push Your Changes**: Push your changes to your fork on GitHub: `git push origin your-branch-name`

9. **Submit a Pull Request**: Navigate to your fork on GitHub and click the "Compare & pull request" button to create a pull request. Fill out the pull request template with the necessary information and submit your request.


## Questions and Support

If you have any questions or need help with the contribution process, feel free to [open an issue](https://github.com/PlatformPlatform/platformplatform/issues/new) in the PlatformPlatform repository.
