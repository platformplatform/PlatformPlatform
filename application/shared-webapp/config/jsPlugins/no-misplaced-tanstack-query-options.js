// Catches TanStack Query options accidentally placed in the init argument (3rd arg) of api.useQuery()
// instead of the options argument (4th arg). The openapi-react-query InitWithUnknowns type silently
// accepts any extra keys via { [key: string]: unknown }, so TypeScript will not catch this mistake.
//
// Correct:   api.useQuery("get", "/api/path", { params: {...} }, { enabled: true })
// Incorrect: api.useQuery("get", "/api/path", { params: {...}, enabled: true })

const TANSTACK_QUERY_OPTIONS = [
  "enabled",
  "staleTime",
  "gcTime",
  "retry",
  "select",
  "placeholderData",
  "throwOnError",
  "refetchInterval",
];

const rule = {
  meta: {
    type: "problem",
    docs: {
      description:
        "Disallow TanStack Query options in the init argument (3rd arg) of api.useQuery()",
    },
  },
  create(context) {
    return {
      CallExpression(node) {
        // Check if this is api.useQuery(...)
        if (
          node.callee.type !== "MemberExpression" ||
          node.callee.object.type !== "Identifier" ||
          node.callee.object.name !== "api" ||
          node.callee.property.type !== "Identifier" ||
          node.callee.property.name !== "useQuery"
        ) {
          return;
        }

        // We need at least 3 arguments: method, path, init
        if (node.arguments.length < 3) {
          return;
        }

        const initArg = node.arguments[2];

        // Check if the 3rd argument is an object expression
        if (initArg.type !== "ObjectExpression") {
          return;
        }

        // Look for TanStack Query options in the init object
        for (const property of initArg.properties) {
          // Skip spread elements
          if (property.type === "SpreadElement") {
            continue;
          }

          // Get the property key name
          let keyName = null;
          if (property.key.type === "Identifier") {
            keyName = property.key.name;
          } else if (property.key.type === "Literal") {
            keyName = property.key.value;
          }

          // Check if this is a TanStack Query option
          if (keyName && TANSTACK_QUERY_OPTIONS.includes(keyName)) {
            context.report({
              node: property,
              message: `TanStack Query option \`${keyName}\` is in the init argument (3rd arg). Move it to the options argument (4th arg) of api.useQuery().`,
            });
          }
        }
      },
    };
  },
};

const plugin = {
  meta: {
    name: "tanstack-query",
  },
  rules: {
    "no-misplaced-options": rule,
  },
};

export default plugin;
