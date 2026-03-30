// Signature pad canvas drawing — JS interop for Blazor
window.signaturePad = {
    _instances: new Map(),

    init: function (canvasElement, dotNetRef) {
        const ctx = canvasElement.getContext('2d');
        const dpr = window.devicePixelRatio || 1;
        let drawing = false;
        let lastPoint = null;

        const resize = () => {
            const rect = canvasElement.getBoundingClientRect();
            canvasElement.width = rect.width * dpr;
            canvasElement.height = rect.height * dpr;
            ctx.scale(dpr, dpr);
            ctx.strokeStyle = '#1a1a1a';
            ctx.lineWidth = 3;
            ctx.lineCap = 'round';
            ctx.lineJoin = 'round';
        };

        resize();
        window.addEventListener('resize', resize);

        const getPos = (e) => {
            const rect = canvasElement.getBoundingClientRect();
            const touch = e.touches ? e.touches[0] : e;
            return { x: touch.clientX - rect.left, y: touch.clientY - rect.top };
        };

        const startDraw = (e) => {
            e.preventDefault();
            drawing = true;
            lastPoint = getPos(e);
        };

        const draw = (e) => {
            e.preventDefault();
            if (!drawing) return;
            const pos = getPos(e);
            ctx.beginPath();
            ctx.moveTo(lastPoint.x, lastPoint.y);
            ctx.lineTo(pos.x, pos.y);
            ctx.stroke();
            lastPoint = pos;
        };

        const stopDraw = (e) => {
            e.preventDefault();
            drawing = false;
        };

        canvasElement.addEventListener('mousedown', startDraw);
        canvasElement.addEventListener('mousemove', draw);
        canvasElement.addEventListener('mouseup', stopDraw);
        canvasElement.addEventListener('mouseleave', stopDraw);
        canvasElement.addEventListener('touchstart', startDraw, { passive: false });
        canvasElement.addEventListener('touchmove', draw, { passive: false });
        canvasElement.addEventListener('touchend', stopDraw, { passive: false });

        this._instances.set(canvasElement, { ctx, resize, dpr });
    },

    clear: function (canvasElement) {
        const instance = this._instances.get(canvasElement);
        if (!instance) return;
        const rect = canvasElement.getBoundingClientRect();
        instance.ctx.clearRect(0, 0, rect.width, rect.height);
    },

    isEmpty: function (canvasElement) {
        const ctx = canvasElement.getContext('2d');
        const pixels = ctx.getImageData(0, 0, canvasElement.width, canvasElement.height).data;
        for (let i = 3; i < pixels.length; i += 4) {
            if (pixels[i] > 0) return false;
        }
        return true;
    },

    toDataUrl: function (canvasElement) {
        return canvasElement.toDataURL('image/png');
    },

    dispose: function (canvasElement) {
        this._instances.delete(canvasElement);
    }
};
