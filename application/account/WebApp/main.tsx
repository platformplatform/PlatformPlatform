/**
 * This file serves as the entry point for the SPA, dynamically importing the main application module `bootstrap.tsx`.
 * Separating the files ensures they are loaded correctly in a Module Federation (micro frontend) setup.
 *
 * Using Module Federation to build micro frontends, this approach allows one micro frontend to dynamically load UI
 * from another micro frontend at runtime (e.g. the User Profile editor dialog).
 *
 * This enables independent development and deployment of shared UI without having to use NPM packages or deploy all
 * frontends when the UI is updated.
 */
import("./bootstrap");
