// Browser print fallback - opens the native print dialog with the receipt rendered as a PNG image.
// Used when Bluetooth is disconnected so the user can print to PDF or a serial printer.
// Resolves only after the print dialog has been dismissed, so callers can safely navigate afterwards.
window.printThermalImage = function (base64Png) {
    return new Promise((resolve) => {
        try {
            const iframe = document.createElement('iframe');
            iframe.style.position = 'fixed';
            iframe.style.right = '0';
            iframe.style.bottom = '0';
            iframe.style.width = '0';
            iframe.style.height = '0';
            iframe.style.border = 'none';
            document.body.appendChild(iframe);

            let settled = false;
            const finish = (ok) => {
                if (settled)
                    return;
                settled = true;
                if (iframe.parentNode)
                    iframe.parentNode.removeChild(iframe);
                resolve(ok);
            };

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

            const img = iframe.contentDocument.querySelector('img');
            const doPrint = () => {
                try {
                    iframe.contentWindow.addEventListener('afterprint', () => setTimeout(() => finish(true), 500));
                    iframe.contentWindow.focus();
                    iframe.contentWindow.print();
                    setTimeout(() => finish(true), 1000);
                }
                catch (error) {
                    console.error('Browser print failed:', error);
                    finish(false);
                }
            };

            if (img.complete)
                doPrint();
            else {
                img.onload = doPrint;
                img.onerror = () => finish(false);
            }
        }
        catch (error) {
            console.error('Browser print failed:', error);
            resolve(false);
        }
    });
};
