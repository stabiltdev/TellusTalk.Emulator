// The page works without this script as plain linked pages; it adds moving between them in place, live updates,
// deleting, the theme switch, local times and new marks.
(function () {
  "use strict";

  var themeKey = "tellustalk-emulator.theme";
  var seenKey = "tellustalk-emulator.seen";

  function read(key) {
    try {
      return localStorage.getItem(key);
    } catch (error) {
      return null;
    }
  }

  function write(key, value) {
    try {
      localStorage.setItem(key, value);
    } catch (error) {
      // Storage unavailable: the choice lasts until the page is left.
    }
  }

  // The theme.

  function applyTheme(choice) {
    var theme = choice === "light" || choice === "dark" ? choice : "system";
    if (theme === "system") {
      delete document.documentElement.dataset.theme;
    } else {
      document.documentElement.dataset.theme = theme;
    }

    document.querySelectorAll("[data-theme-choice]").forEach(function (button) {
      button.setAttribute("aria-pressed", String(button.dataset.themeChoice === theme));
    });
  }

  // Deleting.

  function withoutId() {
    var url = new URL(location.href);
    url.searchParams.delete("id");
    return url.toString();
  }

  // The address of a deleted message would show another one, so it is replaced in the history rather than added to.
  function remove(address) {
    fetch(address, { method: "DELETE" }).then(function (response) {
      if (response.ok || response.status === 404) {
        navigate(withoutId(), "replace");
      }
    });
  }

  // A click that asks for a new tab or window is left to the browser.
  function plainClick(event) {
    return event.button === 0 && !event.metaKey && !event.ctrlKey && !event.shiftKey && !event.altKey;
  }

  document.addEventListener("click", function (event) {
    var link = event.target.closest("a.tt-row, a.tt-tab");
    if (link && plainClick(event)) {
      event.preventDefault();
      navigate(link.href, "push");
      return;
    }

    var themeButton = event.target.closest("[data-theme-choice]");
    if (themeButton) {
      write(themeKey, themeButton.dataset.themeChoice);
      applyTheme(themeButton.dataset.themeChoice);
      return;
    }

    var deleteButton = event.target.closest("[data-delete]");
    if (deleteButton) {
      remove("/api/v1/messages/" + encodeURIComponent(deleteButton.dataset.delete));
      return;
    }

    var reportButton = event.target.closest("[data-report]");
    if (reportButton) {
      report(reportButton);
      return;
    }

    if (event.target.closest("[data-clear]")) {
      remove("/api/v1/messages");
    }
  });

  document.addEventListener("submit", function (event) {
    var form = event.target.closest("#search");
    if (!form) {
      return;
    }

    event.preventDefault();
    var address = new URL(form.action);
    new FormData(form).forEach(function (value, name) {
      if (value) {
        address.searchParams.set(name, value);
      }
    });
    navigate(address.toString(), "push");
  });

  window.addEventListener("popstate", function () {
    navigate(location.href, null);
  });

  // Reports on request. The refresh also comes over the event stream; asking here covers a browser without one.

  function report(button) {
    button.disabled = true;
    fetch("/api/v1/messages/" + encodeURIComponent(button.dataset.id) + "/reports", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ status: button.dataset.report }),
    })
      .then(refresh)
      .finally(function () {
        button.disabled = false;
      });
  }

  // Local times and day headings.

  var timeFormat = new Intl.DateTimeFormat(undefined, { hour: "2-digit", minute: "2-digit" });
  var dateFormat = new Intl.DateTimeFormat(undefined, { year: "numeric", month: "long", day: "numeric" });

  function dayName(date) {
    var today = new Date();
    var yesterday = new Date(today);
    yesterday.setDate(today.getDate() - 1);

    if (date.toDateString() === today.toDateString()) {
      return "Today";
    }

    if (date.toDateString() === yesterday.toDateString()) {
      return "Yesterday";
    }

    return dateFormat.format(date);
  }

  function localTimes(root) {
    root.querySelectorAll("time[datetime]").forEach(function (time) {
      var date = new Date(time.dateTime);
      if (!isNaN(date)) {
        time.textContent = timeFormat.format(date);
        time.title = date.toLocaleString();
      }
    });
  }

  function dayHeadings(list) {
    var previous = null;
    list.querySelectorAll("a.tt-row").forEach(function (row) {
      var date = new Date(row.dataset.at);
      var day = date.toDateString();
      if (day !== previous) {
        var heading = document.createElement("div");
        heading.className = "tt-day";
        heading.textContent = dayName(date);
        row.before(heading);
        previous = day;
      }
    });
  }

  // New since the last look: ISO times in UTC compare as text.

  var seen = read(seenKey);

  function markNew(list) {
    if (!seen) {
      return;
    }

    list.querySelectorAll("a.tt-row").forEach(function (row) {
      row.classList.toggle("new", row.dataset.at > seen);
    });
  }

  // A scheduled message's time lies ahead; remembering it would mark nothing new until it passed.
  function rememberNewest() {
    var now = new Date();
    var newest = Array.prototype.map
      .call(document.querySelectorAll("#list a.tt-row"), function (row) {
        return row.dataset.at;
      })
      .filter(function (at) {
        return new Date(at) <= now;
      })
      .sort()
      .pop();

    if (newest) {
      write(seenKey, newest);
    }
  }

  document.addEventListener("visibilitychange", function () {
    if (document.visibilityState === "hidden") {
      rememberNewest();
    }
  });

  window.addEventListener("pagehide", rememberNewest);

  function enhance() {
    var list = document.getElementById("list");
    localTimes(document);
    if (list) {
      dayHeadings(list);
      markNew(list);
    }
  }

  // Moving in place and live updates: the page fetches the address it would have loaded and swaps in what changed.

  function load(address) {
    return fetch(address, { headers: { Accept: "text/html" } })
      .then(function (response) {
        return response.ok ? response.text() : null;
      })
      .then(function (html) {
        return html === null ? null : new DOMParser().parseFromString(html, "text/html");
      });
  }

  // The list scrolls on its own, and a swapped-in list would start at its top.
  function replace(next, id) {
    var current = document.getElementById(id);
    var fresh = next.getElementById(id);
    if (current && fresh) {
      var scrolled = current.scrollTop;
      current.replaceWith(fresh);
      fresh.scrollTop = scrolled;
    }
  }

  function syncSearch(next) {
    ["tab", "q"].forEach(function (name) {
      var current = document.querySelector('#search input[name="' + name + '"]');
      var fresh = next.querySelector('#search input[name="' + name + '"]');
      if (current && fresh) {
        current.value = fresh.value;
      }
    });
  }

  // Focus on a swapped-out link would fall back to the page; it moves to the link now selected instead.
  function focusAfterSwap(focusedIn) {
    var selector = focusedIn === "list" ? "#list a.tt-row.sel" : focusedIn === "tabs" ? "#tabs a.on" : null;
    var target = selector && document.querySelector(selector);
    if (target) {
      target.focus({ preventScroll: true });
    }
  }

  // Counts navigations, so a slower answer to an earlier one is dropped rather than shown over a later one.
  var navigation = 0;

  // entry is "push" for a new history entry, "replace" to change the current one, and null when the browser moved itself.
  function navigate(address, entry) {
    var current = ++navigation;
    var active = document.activeElement;
    var focusedIn = null;
    if (active && active.closest("#list")) {
      focusedIn = "list";
    } else if (active && active.closest("#tabs")) {
      focusedIn = "tabs";
    }

    load(address)
      .then(function (next) {
        if (current !== navigation) {
          return;
        }

        if (next === null) {
          location.assign(address);
          return;
        }

        if (entry === "push") {
          history.pushState(null, "", address);
        } else if (entry === "replace") {
          history.replaceState(null, "", address);
        }

        // What was listed counts as seen once the reader moves on to another message or list.
        rememberNewest();
        seen = read(seenKey);

        replace(next, "tabs");
        replace(next, "list");
        replace(next, "detail");
        document.getElementById("detail").scrollTop = 0;
        syncSearch(next);
        enhance();
        focusAfterSwap(focusedIn);
      })
      .catch(function () {
        location.assign(address);
      });
  }

  var refreshing = false;
  var again = false;

  function refresh() {
    if (refreshing) {
      again = true;
      return;
    }

    var startedAt = navigation;

    // The page is asked for the message shown, so the refreshed list highlights the one the detail keeps showing.
    var shownButton = document.querySelector("#detail [data-delete]");
    var shown = shownButton ? shownButton.dataset.delete : null;
    var address = new URL(location.href);
    if (shown) {
      address.searchParams.set("id", shown);
    }

    refreshing = true;
    load(address)
      .then(function (next) {
        if (next === null) {
          return;
        }

        // A move made meanwhile shows another address; this answer would undo it, so the refresh is asked for again.
        if (startedAt !== navigation) {
          again = true;
          return;
        }

        var stillListed = shown && next.querySelector('#list a.tt-row[data-id="' + CSS.escape(shown) + '"]');

        replace(next, "tabs");
        replace(next, "list");
        if (stillListed) {
          replace(next, "reports");
        } else {
          replace(next, "detail");
        }

        enhance();
      })
      .finally(function () {
        refreshing = false;
        if (again) {
          again = false;
          refresh();
        }
      });
  }

  // A page left behind can keep its stream open for half a minute or more, and over HTTP/1.1 a browser opens only six
  // connections to a host, so a few quick page changes leave the next page waiting. The stream closes when the page is
  // hidden and reopens if the page is shown again from the back/forward cache.
  var events = null;

  function listen() {
    if (window.EventSource && !events) {
      events = new EventSource("/api/v1/events");
      events.addEventListener("change", refresh);
    }
  }

  window.addEventListener("pagehide", function () {
    if (events) {
      events.close();
      events = null;
    }
  });

  window.addEventListener("pageshow", function (event) {
    if (event.persisted) {
      listen();
      refresh();
    }
  });

  listen();
  applyTheme(read(themeKey));
  enhance();
})();
