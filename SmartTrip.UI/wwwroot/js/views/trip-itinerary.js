document.addEventListener("DOMContentLoaded", function () {
  var modalElement = document.getElementById("editItemModal");
  if (!modalElement || typeof bootstrap === "undefined") {
    return;
  }

  var editModal = new bootstrap.Modal(modalElement);

  document.addEventListener("click", function (event) {
    var editButton = event.target.closest("[data-action='edit-itinerary-item']");
    if (editButton) {
      document.getElementById("editItemId").value = editButton.dataset.itemId || "";
      document.getElementById("editItemTitle").value = editButton.dataset.itemTitle || "";
      document.getElementById("editItemDesc").value = editButton.dataset.itemDesc || "";
      editModal.show();
      return;
    }

    var saveButton = event.target.closest("[data-action='save-itinerary-item']");
    if (saveButton) {
      var id = document.getElementById("editItemId").value;
      var title = document.getElementById("editItemTitle").value;
      var desc = document.getElementById("editItemDesc").value;

      if (typeof $ === "undefined") {
        return;
      }

      $.post("/Trip/UpdateItineraryItem", { id: id, title: title, description: desc }, function () {
        var item = document.getElementById("item-" + id);
        if (item) {
          var titleEl = item.querySelector(".item-title");
          var descEl = item.querySelector(".item-desc");
          if (titleEl) {
            titleEl.textContent = title;
          }
          if (descEl) {
            descEl.textContent = desc;
          }
        }
        editModal.hide();
      });
      return;
    }

    var deleteButton = event.target.closest("[data-action='delete-itinerary-item']");
    if (deleteButton) {
      var itemId = deleteButton.dataset.itemId;
      if (!itemId || typeof $ === "undefined") {
        return;
      }

      if (confirm("Ви впевнені?")) {
        $.post("/Trip/DeleteItineraryItem", { id: itemId }, function () {
          var itemRow = document.getElementById("item-" + itemId);
          if (itemRow) {
            $(itemRow).slideUp(function () {
              $(this).remove();
            });
          }
        });
      }
    }
  });
});
