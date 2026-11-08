// Loaded in the head, before the stylesheet paints, so a chosen theme never flashes the system's first.
(function () {
  try {
    var theme = localStorage.getItem("tellustalk-emulator.theme");
    if (theme === "light" || theme === "dark") {
      document.documentElement.dataset.theme = theme;
    }
  } catch (error) {
    // Storage unavailable: the system's theme applies.
  }
})();
