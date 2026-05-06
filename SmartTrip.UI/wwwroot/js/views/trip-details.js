document.addEventListener("DOMContentLoaded", function () {
  var shareButton = document.getElementById("shareInstagramBtn");
  if (!shareButton) {
    return;
  }

  shareButton.addEventListener("click", function () {
    var shareText = shareButton.dataset.shareText || "";
    if (!shareText) {
      return;
    }

    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard
        .writeText(shareText)
        .then(function () {
          alert("Текст скопійовано до буфера обміну. Відкрий Instagram і встав текст у пост.");
          window.open("https://www.instagram.com/", "_blank");
        })
        .catch(function () {
          alert("Не вдалося скопіювати текст. Відкрийте Instagram та вставте вручну.");
          window.open("https://www.instagram.com/", "_blank");
        });
    } else {
      alert("Ваш браузер не підтримує автоматичне копіювання. Відкрийте Instagram і вставте посилання вручну.");
      window.open("https://www.instagram.com/", "_blank");
    }
  });
});
