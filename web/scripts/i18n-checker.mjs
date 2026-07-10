import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

import ts from "typescript";

const visibleAttributes = new Set([
  "alt",
  "aria-label",
  "description",
  "emptyMessage",
  "label",
  "placeholder",
  "title",
]);

export function compareMessageCatalogs(catalogs) {
  const flattened = Object.fromEntries(
    Object.entries(catalogs).map(([locale, catalog]) => [locale, flattenMessages(catalog)]),
  );
  const allKeys = [...new Set(Object.values(flattened).flatMap((catalog) => [...catalog.keys()]))].sort();
  const issues = [];

  for (const locale of Object.keys(flattened).sort()) {
    const catalog = flattened[locale];
    for (const key of [...catalog.keys()].sort()) {
      if (catalog.get(key)?.trim().length === 0) {
        issues.push(`${locale} has an empty value for ${key}`);
      }
    }
    for (const key of allKeys) {
      if (!catalog.has(key)) {
        issues.push(`${locale} is missing key ${key}`);
      }
    }
  }

  return issues;
}

export function findHardcodedJsx(sourceText, fileName) {
  const sourceFile = ts.createSourceFile(
    fileName,
    sourceText,
    ts.ScriptTarget.Latest,
    true,
    ts.ScriptKind.TSX,
  );
  const issues = [];

  function addIssue(node, rawText) {
    const text = normalizeVisibleText(rawText);
    if (!containsWord(text) || hasIgnoreDirective(sourceText, sourceFile, node)) {
      return;
    }
    const position = sourceFile.getLineAndCharacterOfPosition(node.getStart(sourceFile));
    issues.push({
      file: fileName,
      line: position.line + 1,
      column: position.character + 1,
      text,
    });
  }

  function visit(node) {
    if (ts.isJsxText(node)) {
      addIssue(node, node.getText(sourceFile));
    }

    if (ts.isJsxAttribute(node) && visibleAttributes.has(node.name.text)) {
      const initializer = node.initializer;
      if (initializer && ts.isStringLiteral(initializer)) {
        addIssue(initializer, initializer.text);
      } else if (initializer && ts.isJsxExpression(initializer) && initializer.expression) {
        if (ts.isStringLiteral(initializer.expression) || ts.isNoSubstitutionTemplateLiteral(initializer.expression)) {
          addIssue(initializer.expression, initializer.expression.text);
        } else if (ts.isTemplateExpression(initializer.expression)) {
          const text = [
            initializer.expression.head.text,
            ...initializer.expression.templateSpans.map((span) => span.literal.text),
          ].join(" ");
          addIssue(initializer.expression, text);
        }
      }
    }

    ts.forEachChild(node, visit);
  }

  visit(sourceFile);
  return issues.sort((left, right) => left.line - right.line || left.column - right.column);
}

function flattenMessages(value, prefix = "", result = new Map()) {
  if (typeof value === "string") {
    result.set(prefix, value);
    return result;
  }
  if (!value || typeof value !== "object" || Array.isArray(value)) {
    throw new TypeError(`Message catalog value at '${prefix || "<root>"}' must be an object or string.`);
  }
  for (const [key, child] of Object.entries(value)) {
    flattenMessages(child, prefix ? `${prefix}.${key}` : key, result);
  }
  return result;
}

function normalizeVisibleText(value) {
  return value.replace(/\s+/g, " ").trim();
}

function containsWord(value) {
  return /[\p{L}]/u.test(value);
}

function hasIgnoreDirective(sourceText, sourceFile, node) {
  const position = sourceFile.getLineAndCharacterOfPosition(node.getStart(sourceFile));
  const lines = sourceText.split(/\r?\n/);
  const context = lines.slice(Math.max(0, position.line - 2), position.line + 1).join("\n");
  return /i18n-ignore:\s*[^\s}][^\n}]*/.test(context);
}

function walkTsxFiles(root) {
  if (!fs.existsSync(root)) {
    return [];
  }
  return fs.readdirSync(root, { withFileTypes: true }).flatMap((entry) => {
    const fullPath = path.join(root, entry.name);
    if (entry.isDirectory()) {
      return walkTsxFiles(fullPath);
    }
    return entry.isFile() && entry.name.endsWith(".tsx") && !entry.name.endsWith(".test.tsx")
      ? [fullPath]
      : [];
  });
}

function runCli() {
  const webRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
  const messageDirectory = path.join(webRoot, "messages");
  const catalogs = Object.fromEntries(
    fs.readdirSync(messageDirectory)
      .filter((name) => name.endsWith(".json"))
      .sort()
      .map((name) => [
        path.basename(name, ".json"),
        JSON.parse(fs.readFileSync(path.join(messageDirectory, name), "utf8")),
      ]),
  );

  const catalogIssues = compareMessageCatalogs(catalogs).map((message) => `messages: ${message}`);
  const jsxIssues = [path.join(webRoot, "app"), path.join(webRoot, "components")]
    .flatMap(walkTsxFiles)
    .sort()
    .flatMap((file) => findHardcodedJsx(fs.readFileSync(file, "utf8"), path.relative(webRoot, file)))
    .map((issue) => `${issue.file}:${issue.line}:${issue.column} hardcoded user-visible text: "${issue.text}"`);
  const issues = [...catalogIssues, ...jsxIssues];

  if (issues.length > 0) {
    console.error(issues.join("\n"));
    console.error(`i18n check failed with ${issues.length} issue(s).`);
    process.exitCode = 1;
    return;
  }

  console.log(`i18n check passed for ${Object.keys(catalogs).length} locales.`);
}

const invokedPath = process.argv[1] ? path.resolve(process.argv[1]) : "";
if (invokedPath === fileURLToPath(import.meta.url)) {
  runCli();
}
