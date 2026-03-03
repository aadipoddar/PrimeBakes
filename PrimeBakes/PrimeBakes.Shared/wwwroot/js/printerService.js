// Browser print fallback - opens the native print dialog with the receipt rendered as a PNG image.
// Used when Bluetooth is disconnected so the user can print to PDF or a serial printer.
window.printThermalImage = function (base64Png) {
    try {
        const iframe = document.createElement('iframe');
        iframe.style.position = 'fixed';
        iframe.style.right = '0';
        iframe.style.bottom = '0';
        iframe.style.width = '0';
        iframe.style.height = '0';
        iframe.style.border = 'none';
        document.body.appendChild(iframe);

        iframe.contentDocument.write(`
            <html>
            <head>
                <style>
                    @page { margin: 0; size: 80mm auto; }
                    body { margin: 0; padding: 0; }
                    img { width: 80mm; display: block; }
                </style>
            </head>
            <body>
                <img src="data:image/png;base64,${base64Png}" />
            </body>
            </html>
        `);
        iframe.contentDocument.close();

        // Print the iframe content with delay to ensure the image loads
        const img = iframe.contentDocument.querySelector('img');
        const doPrint = () => {
            iframe.contentWindow.focus();
            iframe.contentWindow.print();
            setTimeout(() => document.body.removeChild(iframe), 1000);
        };

        if (img.complete) {
            doPrint();
        } else {
            img.onload = doPrint;
        }

        return true;
    } catch (error) {
        console.error('Browser print failed:', error);
        return false;
    }
};
