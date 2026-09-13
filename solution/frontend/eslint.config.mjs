import nextPlugin from "@next/eslint-plugin-next";
import { defineConfig, globalIgnores } from "eslint/config";
import reactHooks from "eslint-plugin-react-hooks";
import tseslint from "typescript-eslint";

// ESLint 10 flat config. eslint-config-next is not used: it bundles plugins that only support ESLint 9.
export default defineConfig(
  globalIgnores([".next/**", "next-env.d.ts"]),
  tseslint.configs.recommended,
  reactHooks.configs.flat["recommended-latest"],
  nextPlugin.configs["core-web-vitals"],
);
