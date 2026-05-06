document.addEventListener("DOMContentLoaded", function () {
  var isAuthenticated = document.body && document.body.dataset.authenticated === "true";
  if (!isAuthenticated) {
    return;
  }

  var toastLiveExample = document.getElementById("liveToast");
  var toastTitle = document.getElementById("toastTitle");
  var toastMessage = document.getElementById("toastMessage");

  if (!toastLiveExample || !toastTitle || !toastMessage) {
    return;
  }

  if (typeof signalR === "undefined" || typeof bootstrap === "undefined") {
    return;
  }

  var connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .withAutomaticReconnect()
    .build();

  connection.on("ReceiveNotification", function (title, message) {
    toastTitle.textContent = title;
    toastMessage.textContent = message;

    var toastBootstrap = bootstrap.Toast.getOrCreateInstance(toastLiveExample);
    toastBootstrap.show();
  });

  connection.start().catch(function () {
    // Silent fail to avoid noisy UI errors when SignalR is unavailable.
  });
});
