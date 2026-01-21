// Order Button Animation Script
// Handles the animated delivery truck button

window.OrderButtonHelper = {
	// Start animation on button click
	startAnimation: function (buttonElement) {
		if (!buttonElement) return false;

		if (!buttonElement.classList.contains('animate')) {
			buttonElement.classList.add('animate');
			return true;
		}
		return false;
	},

	// Reset animation
	resetAnimation: function (buttonElement) {
		if (!buttonElement) return;
		buttonElement.classList.remove('animate');
	},

	// Initialize button by id
	initializeButton: function (buttonId) {
		var button = document.getElementById(buttonId);
		if (button) {
			return true;
		}
		return false;
	}
};
