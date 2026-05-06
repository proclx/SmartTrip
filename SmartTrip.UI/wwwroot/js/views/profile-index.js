document.addEventListener("DOMContentLoaded", function () {
  var modalElement = document.getElementById("editModal");
  if (!modalElement || typeof bootstrap === "undefined") {
    return;
  }

  var editModal = new bootstrap.Modal(modalElement);

  document.addEventListener("click", function (event) {
    var trigger = event.target.closest("[data-action='edit-default-item']");
    if (!trigger) {
      return;
    }

    var itemId = trigger.dataset.itemId;
    var itemName = trigger.dataset.itemName;
    var itemCategory = trigger.dataset.itemCategory;

    var idInput = document.getElementById("editItemId");
    var nameInput = document.getElementById("editItemName");
    var categoryInput = document.getElementById("editItemCategory");

    if (idInput) {
      idInput.value = itemId || "";
    }
    if (nameInput) {
      nameInput.value = itemName || "";
    }
    if (categoryInput) {
      categoryInput.value = itemCategory || "";
    }

    editModal.show();
  });
});
