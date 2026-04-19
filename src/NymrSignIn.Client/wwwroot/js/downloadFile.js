window.downloadBase64File = (fileName, mimeType, base64Content) => {
    const a = document.createElement('a');
    a.href = `data:${mimeType};base64,${base64Content}`;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
};
