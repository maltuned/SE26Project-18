const { defineConfig } = require("eslint/config");
const expoConfig = require("eslint-config-expo/flat");

module.exports = defineConfig([
  expoConfig,
  {
    ignores: ["dist/*", "**/src/api/backend-sim.ts"],
    rules: {
      "react-hooks/set-state-in-effect": "off",
    },
  },
]);
