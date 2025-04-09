const fs = require('fs');
const path = require('path');

const ROOT = '.';
const EXPERIMENTAL = '.Experimental';
const README_PATH = 'README.md';
const MARKER_START = '<!-- BEGIN MOD LIST -->';
const MARKER_END = '<!-- END MOD LIST -->';

function getModFolders(baseDir) {
  const entries = fs.readdirSync(baseDir, { withFileTypes: true });
  return entries
    .filter(entry => entry.isDirectory())
    .map(entry => path.join(baseDir, entry.name))
    .filter(dir => fs.existsSync(path.join(dir, 'README.md')));
}

function formatTable(mods, baseDir) {
  if (mods.length === 0) return '';

  let rows = mods.map(modPath => {
    const modName = path.basename(modPath);
    const readmeLink = path.join(modPath, 'README.md');
    const zipLink = path.join(modPath, `${modName}.zip`);
    return `| [${modName}](${readmeLink}) | [Download](${zipLink}) |`;
  });

  return [
    `### ${baseDir === EXPERIMENTAL ? 'Experimental Mods' : 'Released Mods'}`,
    '',
    '| Name | Download |',
    '|------|----------|',
    ...rows,
    ''
  ].join('\n');
}

function updateReadme(modListSection) {
  const readme = fs.readFileSync(README_PATH, 'utf8');
  const before = readme.split(MARKER_START)[0];
  const after = readme.split(MARKER_END)[1];

  const newReadme = `${before}${MARKER_START}\n\n${modListSection}\n${MARKER_END}${after}`;
  fs.writeFileSync(README_PATH, newReadme);
}

const mainMods = getModFolders(ROOT).filter(dir => !dir.startsWith(EXPERIMENTAL));
const experimentalMods = getModFolders(EXPERIMENTAL);

const tableContent = [
  formatTable(mainMods, ROOT),
  formatTable(experimentalMods, EXPERIMENTAL)
].join('\n');

updateReadme(tableContent);