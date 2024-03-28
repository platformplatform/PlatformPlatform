const antfu = require("@antfu/eslint-config").default;

module.exports = antfu({
  stylistic: {
    quotes: "double",
    semi: true,
  },
  typescript: true,
  react: true,
  formatters: {
    css: true,
    html: true,
    markdown: true,
  },
  rules: {
    "style/comma-dangle": ["error", {
      arrays: "always-multiline",
      objects: "always-multiline",
      imports: "always-multiline",
      exports: "always-multiline",
      functions: "never",
    }],
    "style/jsx-one-expression-per-line": "off",
    "style/jsx-closing-tag-location": "off",
    "style/max-len": ["error", {
      code: 120,
      ignoreComments: true,
      ignoreStrings: true,
      ignoreTemplateLiterals: true,
      ignoreUrls: true,
    }],
    "style/member-delimiter-style": ["error", {
      multiline: {
        delimiter: "comma",
        requireLast: true,
      },
      singleline: {
        delimiter: "comma",
        requireLast: true,
      },
      overrides: {
        interface: {
          multiline: {
            delimiter: "semi",
            requireLast: true,
          },
        },
      },
    }],
  },
}, {
  // Allow disabling eslint rules with comments for generated files
  files: ["**/locale/*.ts"],
  rules: {
    "eslint-comments/no-unlimited-disable": "off",
  },
}, {
  // Ignore generated files
  ignores: [
    "**/*.generated.d.ts",
    "**/*.generated.ts",
    "**/.swc/*",
    "**/dist/*",
    "**/locale/*.d.ts",
    "**/lib/api/Api.json",
  ],
});
