// "Are you sure you want to close?" guard for the web app. We manage the beforeunload listener
// ourselves so an intentional close can drop it and close in one call, with no prompt.
window.pageCloseGuard = {
	_handler: function (e) { e.preventDefault(); e.returnValue = ''; },

	// Adding the same handler twice is a no-op, so block/unblock are safe to call repeatedly.
	block: function () { window.addEventListener('beforeunload', this._handler); },
	unblock: function () { window.removeEventListener('beforeunload', this._handler); },

	// Drop the guard, then close next tick so the in-flight Blazor call finishes first.
	close: function () { this.unblock(); setTimeout(function () { window.close(); }, 0); }
};
