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

function extractDescription(readmePath) {
  try {
    const content = fs.readFileSync(readmePath, 'utf8');
    const lines = content.split('\n');
   
    // Find the first header (# something)
    let headerIndex = -1;
    for (let i = 0; i < lines.length; i++) {
      if (lines[i].trim().startsWith('# ')) {
        headerIndex = i;
        break;
      }
    }
   
    // If we found a header, look for the first non-empty line after it
    if (headerIndex !== -1) {
      for (let i = headerIndex + 1; i < lines.length; i++) {
        const line = lines[i].trim();
        if (line && !line.startsWith('#')) {
          return line;
        }
      }
    }
   
    return 'No description available';
  } catch (error) {
    console.error(`Error reading ${readmePath}:`, error);
    return 'No description available';
  }
}

function checkIfDllExists(modPath, modName) {
  const dllPath = path.join(modPath, `${modName}.dll`);
  return fs.existsSync(dllPath);
}

function formatTable(mods, baseDir) {
  if (mods.length === 0) return '';
  
  let rows = mods.map(modPath => {
    const modName = path.basename(modPath);
    const readmeLink = path.join(modPath, 'README.md');
    const readmePath = path.join(modPath, 'README.md');
    const description = extractDescription(readmePath);
    
    // Check if DLL exists and format download cell accordingly
    const hasDll = checkIfDllExists(modPath, modName);
    const downloadCell = hasDll 
      ? `[Download](${path.join(modPath, `${modName}.dll`)})`
      : 'No Download';
   
    return `| [${modName}](${readmeLink}) | ${description} | ${downloadCell} |`;
  });
  
  return [
    `### ${baseDir === EXPERIMENTAL ? 'Experimental Mods' : 'Released Mods'}`,
    '',
    '| Name | Description | Download |',
    '|------|-------------|----------|',
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