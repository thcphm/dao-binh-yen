// Ghi file luu (persistentDataPath = IDBFS) xuong IndexedDB cua trinh duyet ngay sau khi luu.
// Khong goi thi file chi nam trong bo nho, tai lai trang la mat.
mergeInto(LibraryManager.library, {
  SereneSyncFS: function () {
    if (typeof FS === 'undefined' || !FS.syncfs) return;
    FS.syncfs(false, function (err) { if (err) console.warn('[SereneIsle] Khong ghi duoc file luu vao IndexedDB:', err); });
  }
});
