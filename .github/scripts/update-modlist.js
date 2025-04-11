const fs = require('fs');
const path = require('path');
const https = require('https');

// Configuration
const ROOT = '.';
const EXPERIMENTAL = '.Experimental';
const README_PATH = 'README.md';
const MARKER_START = '<!-- BEGIN MOD LIST -->';
const MARKER_END = '<!-- END MOD LIST -->';
const REPO_OWNER = process.env.REPO_OWNER || 'NotAKidoS';
const REPO_NAME = process.env.REPO_NAME || 'NAK_CVR_Mods';

// Function to get latest release info from GitHub API
async function getLatestRelease() {
  return new Promise((resolve, reject) => {
    const options = {
      hostname: 'api.github.com',
      path: `/repos/${REPO_OWNER}/${REPO_NAME}/releases/latest`,
      method: 'GET',
      headers: {
        'User-Agent': 'Node.js GitHub Release Checker',
        'Accept': 'application/vnd.github.v3+json'
      }
    };

    const req = https.request(options, (res) => {
      let data = '';
      
      res.on('data', (chunk) => {
        data += chunk;
      });
      
      res.on('end', () => {
        if (res.statusCode === 200) {
          try {
            resolve(JSON.parse(data));
          } catch (e) {
            reject(new Error(`Failed to parse GitHub API response: ${e.message}`));
          }
        } else {
          reject(new Error(`GitHub API request failed with status code: ${res.statusCode}`));
        }
      });
    });
    
    req.on('error', (e) => {
      reject(new Error(`GitHub API request error: ${e.message}`));
    });
    
    req.end();
  });
}

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

async function formatTable(mods, baseDir) {
  if (mods.length === 0) return '';
  
  try {
    // Get the latest release info from GitHub
    const latestRelease = await getLatestRelease();
    const releaseAssets = latestRelease.assets || [];
    
    // Create a map of available files in the release
    const availableFiles = {};
    releaseAssets.forEach(asset => {
      availableFiles[asset.name] = asset.browser_download_url;
    });
    
    let rows = mods.map(modPath => {
      const modName = path.basename(modPath);
      const readmeLink = path.join(modPath, 'README.md');
      const readmePath = path.join(modPath, 'README.md');
      const description = extractDescription(readmePath);
      
      // Check if the DLL exists in the latest release
      const dllFilename = `${modName}.dll`;
      let downloadSection;
      
      if (availableFiles[dllFilename]) {
        downloadSection = `[Download](${availableFiles[dllFilename]})`;
      } else {
        downloadSection = 'No Download';
      }
      
      return `| [${modName}](${readmeLink}) | ${description} | ${downloadSection} |`;
    });
    
    return [
      `### ${baseDir === EXPERIMENTAL ? 'Experimental Mods' : 'Released Mods'}`,
      '',
      '| Name | Description | Download |',
      '|------|-------------|----------|',
      ...rows,
      ''
    ].join('\n');
  } catch (error) {
    console.error('Error fetching release information:', error);
    
    // Fallback to showing "No Download" for all mods if we can't fetch release info
    let rows = mods.map(modPath => {
      const modName = path.basename(modPath);
      const readmeLink = path.join(modPath, 'README.md');
      const readmePath = path.join(modPath, 'README.md');
      const description = extractDescription(readmePath);
      
      return `| [${modName}](${readmeLink}) | ${description} | No Download |`;
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
}

function updateReadme(modListSection) {
  const readme = fs.readFileSync(README_PATH, 'utf8');
  const before = readme.split(MARKER_START)[0];
  const after = readme.split(MARKER_END)[1];
  const newReadme = `${before}${MARKER_START}\n\n${modListSection}\n${MARKER_END}${after}`;
  fs.writeFileSync(README_PATH, newReadme);
}

async function main() {
  try {
    const mainMods = getModFolders(ROOT).filter(dir => !dir.startsWith(EXPERIMENTAL));
    const experimentalMods = getModFolders(EXPERIMENTAL);
    
    const mainModsTable = await formatTable(mainMods, ROOT);
    const experimentalModsTable = await formatTable(experimentalMods, EXPERIMENTAL);
    
    const tableContent = [mainModsTable, experimentalModsTable].join('\n');
    updateReadme(tableContent);
    
    console.log('README.md updated successfully!');
  } catch (error) {
    console.error('Error updating README:', error);
    process.exit(1);
  }
}

main();