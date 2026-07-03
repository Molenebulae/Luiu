import Dropzone from "dropzone";

Dropzone.autoDiscover = false;

const FILE_STRATEGIES = {
    image: {
        acceptedFiles: "image/jpeg,image/png,image/webp",
        dictDefaultMessage: "拖曳圖片到這裡，或點擊選擇圖片",
        dictmaxFilesExceeded: "圖片超出上限",
        maxFilesSize: 5, // 5MB
    },
};

export function useDropzene() {
    const getBaseOptions = () => ({
        autoProcessQueue: false,
        addRemoveLinks: true,
        parallelUploads: 5,
        dictRemoveFile: "移除",
        dictCancelUpload: "取消上傳",
        dictFileTooBig: "檔案過大 ({{filesize}}MiB)。上限: {{maxFilesize}}MiB。",
        renameFile: (file) => `${new Date().getTime()}_${file.name.replace(/\s/g, "_")}`
    });

    const initDropzene = (element, { type = 'image', options = {}}, refreshCallback) => {
        const strategy = FILE_STRATEGIES[type] || FILE_STRATEGIES.image;  // 取得設定策略
        const finalOptions = {
            ...getBaseOptions(),
            ...strategy,
            ...options
        };

        const dz = new Dropzone(element, finalOptions);
        
        // 刪除檔案
        dz.on("removedfile", () => {
            if (dz.files.length <= finalOptions.maxFiles) {
                dz.files.forEach(f => {
                    if (f.status === Dropzone.ERROR) {
                        f.status = Dropzone.QUEUED;
                        f.accepted = true;
                        if (f.previewElement) f.previewElement.classList.remove("dz-error");
                    }
                });
            }
            if (refreshCallback) refreshCallback();
        });

        // 新增檔案
        dz.on("addedfile", (file) => { 
            // 如果加進來後，總數超過了你的上限
            if (dz.files.length > finalOptions.maxFiles) {
                dz.removeFile(file); // 直接把它從 Dropzone 裡強制刪除，連紅色的錯誤框都不給留
                alert(`已達數量上限！最多只能上傳 ${finalOptions.maxFiles} 張照片。`);
            }
            
            // 只要檔案有變動，一樣通知外面的 Vue 更新數字
            if (refreshCallback) refreshCallback(); 
        });

        return dz;
    }

    return { 
        initDropzene
     };
}