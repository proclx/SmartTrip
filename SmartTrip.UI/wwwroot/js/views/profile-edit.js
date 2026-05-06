document.addEventListener("DOMContentLoaded", function () {
  var imageInput = document.getElementById("imageInput");
  var deleteButton = document.getElementById("deleteImageBtn");

  if (imageInput) {
    imageInput.addEventListener("change", function (event) {
      var file = event.target.files[0];
      if (!file) {
        return;
      }

      var reader = new FileReader();
      reader.onload = function (loadEvent) {
        var preview = document.getElementById("preview");
        if (preview) {
          preview.src = loadEvent.target.result;
          return;
        }

        var placeholder = document.getElementById("preview-placeholder");
        if (placeholder) {
          placeholder.innerHTML =
            '<img src="' +
            loadEvent.target.result +
            '" class="profile-image-preview-img" alt="Preview" />';
        }
      };
      reader.readAsDataURL(file);
    });
  }

  if (deleteButton) {
    deleteButton.addEventListener("click", function () {
      if (confirm("Ви впевнені, що хочете видалити фото?")) {
        var deleteForm = document.getElementById("deleteImageForm");
        if (deleteForm) {
          deleteForm.submit();
        }
      }
    });
  }
});
