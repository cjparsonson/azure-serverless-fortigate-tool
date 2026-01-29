// Configuration for the Azure Function endpoint
// URL is defined in config.js
const AZURE_FUNCTION_URL = CONFIG.AZURE_FUNCTION_URL;

// Wait for the DOM to be fully loaded
document.addEventListener('DOMContentLoaded', () => {

    const btn = document.getElementById('btnConvert');
    // Add click event listener to the button
    btn.addEventListener('click', handleConversion);

    // Copy button logic
    const copyBtn = document.getElementById('btnCopy');
    copyBtn.addEventListener('click', () => {
        const outputText = document.getElementById('output').textContent;
        navigator.clipboard.writeText(outputText).then(() => {
            // Visual feedback for successful copy
            copyBtn.textContent = 'Copied!';
            setTimeout(() => {
                copyBtn.textContent = 'Copy to Clipboard';
            }, 2000);
        });
    });

    // Toggle list button logic
    document.getElementById('btnToggleList').addEventListener('click', () => {
        const container = document.getElementById('macListContainer');
        const btn = document.getElementById('btnToggleList');
        if (container.style.display === 'none' || container.style.display === '') {
            container.style.display = 'block';
            btn.textContent = 'Hide List';
        } else {
            container.style.display = 'none';
            btn.textContent = 'Show List';
        }
    });
});

async function handleConversion() {
    const outputDiv = document.getElementById('output');
    const statsArea = document.getElementById('statsArea');
    const macInput = document.getElementById('fmac');
    const fileInput = document.getElementById('fileInput');
    const btnCopy = document.getElementById('btnCopy');

    outputDiv.innerHTML = ''; // Clear previous output
    outputDiv.textContent = 'Processing...';
    outputDiv.classList.remove('error');
    statsArea.style.display = 'none';

    let rawText = macInput.value;

    // 1. If a file is selected, read its content
    if (fileInput.files.length > 0) {
        try {
            rawText = await readFileAsText(fileInput.files[0]);
        } catch (error) {
            outputDiv.textContent = `Error reading file: ${error.message}`;
            outputDiv.classList.add('error');
            return;
        }
    }

    // 2. Prepare payload
    const payload = {
        MacAddressList: rawText,
        FortigateName: document.getElementById('fname').value,
        GroupChoice: parseInt(document.getElementById('fgroup').value)
    };

    try {
        // 3. Call Azure Function
        const response = await fetch(AZURE_FUNCTION_URL, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || response.statusText);
        }

        // 4. Handle JSON response
        const data = await response.json();

        // 5. Update UI with results
        outputDiv.textContent = data.Script;
        document.getElementById('macCount').textContent = data.Count;
        // Build list
        const listContainer = document.getElementById('macListContainer');
        listContainer.innerHTML = data.ExtractedMacs.map(mac => `<div>${mac}</div>`).join('');
        // Show stats
        statsArea.style.display = 'block';
        // Show copy button
        btnCopy.style.display = 'inline-block';

    } catch (error) {
        outputDiv.textContent = `Error: ${error.message}`;
        outputDiv.classList.add('error');
    }
}

// Helper function to read file as text
function readFileAsText(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = (e) => resolve(e.target.result);
        reader.onerror = (e) => reject(e);
        reader.readAsText(file);
    });
}
