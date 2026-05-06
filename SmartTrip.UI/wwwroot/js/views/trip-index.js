document.addEventListener("DOMContentLoaded", function () {
  var modalElement = document.getElementById("packingListModal");
  var modalContent = document.getElementById("packingListModalContent");

  if (!modalElement || !modalContent || typeof bootstrap === "undefined") {
    return;
  }

  var packingModal = new bootstrap.Modal(modalElement);

  function showSpinner() {
    modalContent.innerHTML =
      '<div class="p-5 text-center"><div class="spinner-border text-primary" role="status" aria-label="Loading"></div></div>';
  }

  document.addEventListener("click", function (event) {
    var target = event.target.closest("[data-action]");
    if (!target) {
      return;
    }

    var action = target.dataset.action;
    var tripId = target.dataset.tripId;

    if (action === "open-packing-modal") {
      if (!tripId || typeof $ === "undefined") {
        return;
      }
      showSpinner();
      packingModal.show();
      $.get("/Trip/GetPackingListModal?tripId=" + tripId, function (data) {
        modalContent.innerHTML = data;
      });
      return;
    }

    if (action === "sync-list") {
      if (!tripId || typeof $ === "undefined") {
        return;
      }
      $.post("/Trip/SyncPackingList", { tripId: tripId }, function (data) {
        modalContent.innerHTML = data;
      });
      return;
    }

    if (action === "reset-list") {
      if (!tripId || typeof $ === "undefined") {
        return;
      }
      if (confirm("Точно скинути? Усі відмітки зникнуть!")) {
        $.post("/Trip/ResetPackingList", { tripId: tripId }, function (data) {
          modalContent.innerHTML = data;
        });
      }
      return;
    }

    if (action === "delete-trip-item") {
      var itemId = target.dataset.itemId;
      if (!tripId || !itemId || typeof $ === "undefined") {
        return;
      }
      if (confirm("Ви впевнені, що хочете видалити цю річ?")) {
        $.post("/Trip/DeleteTripPackingItem", { itemId: itemId, tripId: tripId }, function (data) {
          modalContent.innerHTML = data;
        });
      }
    }
  });

  document.addEventListener("change", function (event) {
    var checkbox = event.target.closest("[data-action='toggle-item']");
    if (!checkbox || typeof $ === "undefined") {
      return;
    }

    var itemId = checkbox.dataset.itemId;
    if (!itemId) {
      return;
    }

    var labelSpan = checkbox.closest("label");
    var textSpan = labelSpan ? labelSpan.querySelector("span") : null;

    $.post("/Trip/TogglePackingItem", { itemId: itemId }, function () {
      if (!textSpan) {
        return;
      }
      if (checkbox.checked) {
        textSpan.classList.add("text-decoration-line-through", "text-muted");
      } else {
        textSpan.classList.remove("text-decoration-line-through", "text-muted");
      }
    });
  });

  document.addEventListener("submit", function (event) {
    var form = event.target.closest("[data-action='add-trip-item']");
    if (!form || typeof $ === "undefined") {
      return;
    }

    event.preventDefault();
    var tripId = form.dataset.tripId;
    var nameInput = form.querySelector("#newItemName");
    var categoryInput = form.querySelector("#newItemCategory");

    if (!tripId || !nameInput || !categoryInput) {
      return;
    }

    $.post(
      "/Trip/AddTripPackingItem",
      { tripId: tripId, name: nameInput.value, category: categoryInput.value },
      function (data) {
        modalContent.innerHTML = data;
      }
    );
  });
});
