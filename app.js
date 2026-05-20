'use strict';

const TEMPLATES = {
  html: `<!DOCTYPE html>\n<html lang="ru">\n<head>\n  <meta charset="UTF-8" />\n  <meta name="viewport" content="width=device-width, initial-scale=1.0" />\n  <title>Документ</title>\n</head>\n<body>\n  <h1>Привет, мир!</h1>\n</body>\n</html>`,
  css: `/* Стили */\nbody {\n  margin: 0;\n  font-family: sans-serif;\n  background: #f5f5f5;\n  color: #333;\n}\n`,
  js: `'use strict';\n\n// Точка входа\nfunction main() {\n  console.log('Привет, мир!');\n}\n\nmain();\n`,
  ts: `// TypeScript файл\n\ninterface User {\n  id: number;\n  name: string;\n}\n\nfunction greet(user: User): string {\n  return \`Привет, \${user.name}!\`;\n}\n\nconsole.log(greet({ id: 1, name: 'Алиса' }));\n`,
  py: `#!/usr/bin/env python3\n# -*- coding: utf-8 -*-\n\ndef main():\n    print("Привет, мир!")\n\nif __name__ == "__main__":\n    main()\n`,
  java: `public class Main {\n    public static void main(String[] args) {\n        System.out.println("Привет, мир!");\n    }\n}\n`,
  cpp: `#include <iostream>\n\nint main() {\n    std::cout << "Привет, мир!" << std::endl;\n    return 0;\n}\n`,
  c: `#include <stdio.h>\n\nint main() {\n    printf("Привет, мир!\\n");\n    return 0;\n}\n`,
  cs: `using System;\n\nclass Program {\n    static void Main(string[] args) {\n        Console.WriteLine("Привет, мир!");\n    }\n}\n`,
  go: `package main\n\nimport "fmt"\n\nfunc main() {\n    fmt.Println("Привет, мир!")\n}\n`,
  rs: `fn main() {\n    println!("Привет, мир!");\n}\n`,
  php: `<?php\n\necho "Привет, мир!\\n";\n`,
  rb: `# Ruby\nputs "Привет, мир!"\n`,
  swift: `import Foundation\n\nprint("Привет, мир!")\n`,
  kt: `fun main() {\n    println("Привет, мир!")\n}\n`,
  sh: `#!/bin/bash\n\necho "Привет, мир!"\n`,
  bat: `@echo off\necho Привет, мир!\npause\n`,
  sql: `-- SQL скрипт\nCREATE TABLE users (\n  id   SERIAL PRIMARY KEY,\n  name VARCHAR(100) NOT NULL,\n  email VARCHAR(255) UNIQUE NOT NULL\n);\n\nINSERT INTO users (name, email) VALUES ('Алиса', 'alice@example.com');\n\nSELECT * FROM users;\n`,
  json: `{\n  "name": "Мой проект",\n  "version": "1.0.0",\n  "description": "Описание проекта"\n}\n`,
  xml: `<?xml version="1.0" encoding="UTF-8"?>\n<root>\n  <item id="1">\n    <name>Пример</name>\n    <value>42</value>\n  </item>\n</root>\n`,
  yaml: `name: Мой проект\nversion: 1.0.0\nsettings:\n  debug: false\n  language: ru\n`,
  toml: `[project]\nname = "Мой проект"\nversion = "1.0.0"\n\n[settings]\ndebug = false\nlanguage = "ru"\n`,
  ini: `[General]\nname=Мой проект\nversion=1.0.0\n\n[Settings]\ndebug=false\nlanguage=ru\n`,
  env: `# Переменные окружения\nAPP_NAME=МойПроект\nAPP_ENV=development\nAPP_PORT=3000\nDEBUG=true\n`,
  md: `# Заголовок\n\nЭто **Markdown** документ.\n\n## Раздел\n\n- Пункт 1\n- Пункт 2\n- Пункт 3\n\n## Код\n\n\`\`\`javascript\nconsole.log('Привет!');\n\`\`\`\n`,
  csv: `Имя,Возраст,Город\nАлиса,30,Москва\nБob,25,Санкт-Петербург\nВиктор,35,Новосибирск\n`,
  tex: `\\documentclass{article}\n\\usepackage[utf8]{inputenc}\n\\usepackage[russian]{babel}\n\\title{Мой документ}\n\\author{Автор}\n\\date{\\today}\n\n\\begin{document}\n\\maketitle\n\n\\section{Введение}\nЭто LaTeX документ.\n\n\\end{document}\n`,
  rst: `Заголовок\n=========\n\nЭто reStructuredText документ.\n\nРаздел\n------\n\n- Пункт 1\n- Пункт 2\n\n.. code-block:: python\n\n   print("Привет!")\n`,
  adoc: `= Заголовок\nАвтор\n:lang: ru\n\nЭто AsciiDoc документ.\n\n== Раздел\n\n* Пункт 1\n* Пункт 2\n\n[source,python]\n----\nprint("Привет!")\n----\n`,
  svg: `<svg xmlns="http://www.w3.org/2000/svg" width="200" height="200" viewBox="0 0 200 200">\n  <circle cx="100" cy="100" r="80" fill="#6c63ff" opacity="0.8"/>\n  <text x="100" y="108" text-anchor="middle" font-family="sans-serif"\n        font-size="20" fill="white">FileForge</text>\n</svg>\n`,
  txt: '',
  log: `[2026-05-20 12:00:00] INFO  Приложение запущено\n[2026-05-20 12:00:01] DEBUG Инициализация компонентов\n[2026-05-20 12:00:02] INFO  Сервер слушает порт 3000\n`,
};

const el = id => document.getElementById(id);

const filenameInput = el('filename');
const formatSelect  = el('format');
const customExtInput = el('custom-ext');
const encodingSelect = el('encoding');
const editor        = el('editor');
const filePreview   = el('file-preview');
const charCount     = el('char-count');
const btnDownload   = el('btn-download');
const btnTemplate   = el('btn-template');
const btnClear      = el('btn-clear');
const btnCopy       = el('btn-copy');
const toast         = el('toast');

let toastTimer = null;

function showToast(msg, type = '') {
  toast.textContent = msg;
  toast.className = 'toast show' + (type ? ' ' + type : '');
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => { toast.className = 'toast'; }, 2500);
}

function getExtension() {
  const fmt = formatSelect.value;
  if (fmt === 'custom') {
    return customExtInput.value.replace(/^\.+/, '').trim() || 'txt';
  }
  return fmt;
}

function updatePreview() {
  const name = filenameInput.value.trim() || 'файл';
  const ext  = getExtension();
  filePreview.textContent = `${name}.${ext}`;
}

function updateCharCount() {
  const len = editor.value.length;
  const lines = editor.value === '' ? 0 : editor.value.split('\n').length;
  charCount.textContent = `${len.toLocaleString('ru')} символов · ${lines} строк`;
}

formatSelect.addEventListener('change', () => {
  const isCustom = formatSelect.value === 'custom';
  customExtInput.classList.toggle('hidden', !isCustom);
  if (isCustom) customExtInput.focus();
  updatePreview();
});

customExtInput.addEventListener('input', updatePreview);
filenameInput.addEventListener('input', updatePreview);
editor.addEventListener('input', updateCharCount);

btnTemplate.addEventListener('click', () => {
  const fmt = formatSelect.value === 'custom' ? 'txt' : formatSelect.value;
  const tpl = TEMPLATES[fmt];
  if (tpl === undefined || tpl === '') {
    showToast('Нет шаблона для этого формата');
    return;
  }
  if (editor.value && !confirm('Заменить текущее содержимое шаблоном?')) return;
  editor.value = tpl;
  updateCharCount();
  showToast('Шаблон вставлен', 'success');
});

btnClear.addEventListener('click', () => {
  if (!editor.value) return;
  if (confirm('Очистить редактор?')) {
    editor.value = '';
    updateCharCount();
  }
});

btnCopy.addEventListener('click', async () => {
  if (!editor.value) { showToast('Редактор пуст'); return; }
  try {
    await navigator.clipboard.writeText(editor.value);
    showToast('Скопировано!', 'success');
  } catch {
    showToast('Не удалось скопировать', 'error');
  }
});

editor.addEventListener('keydown', e => {
  if (e.key === 'Tab') {
    e.preventDefault();
    const start = editor.selectionStart;
    const end   = editor.selectionEnd;
    editor.value = editor.value.slice(0, start) + '  ' + editor.value.slice(end);
    editor.selectionStart = editor.selectionEnd = start + 2;
    updateCharCount();
  }
});

function encodeContent(text, encoding) {
  if (encoding === 'utf-8' || encoding === 'utf-16') return text;
  if (encoding === 'ascii') {
    return text.replace(/[^\x00-\x7F]/g, '?');
  }
  return text;
}

btnDownload.addEventListener('click', () => {
  const name     = filenameInput.value.trim() || 'файл';
  const ext      = getExtension();
  const encoding = encodingSelect.value;
  const content  = encodeContent(editor.value, encoding);

  const mimeMap = {
    html: 'text/html', css: 'text/css', js: 'text/javascript',
    ts: 'text/typescript', json: 'application/json',
    xml: 'application/xml', svg: 'image/svg+xml',
    csv: 'text/csv', md: 'text/markdown',
  };
  const mime = mimeMap[ext] || 'text/plain';

  const charsetPart = (encoding === 'utf-8' || encoding === 'utf-16') ? ';charset=utf-8' : '';
  const blob = new Blob([content], { type: mime + charsetPart });
  const url  = URL.createObjectURL(blob);
  const a    = document.createElement('a');
  a.href     = url;
  a.download = `${name}.${ext}`;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);

  showToast(`Файл «${name}.${ext}» скачан`, 'success');
});

updatePreview();
updateCharCount();
