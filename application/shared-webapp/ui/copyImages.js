/**
 * This script copies the images from the images folder to the dist folder.
 * We are waiting for rslib to provide a better solution for this.
 */
const fs = require("node:fs");
const path = require("node:path");

fs.cpSync(path.resolve(__dirname, "images"), path.resolve(__dirname, "dist", "images"), { recursive: true });
