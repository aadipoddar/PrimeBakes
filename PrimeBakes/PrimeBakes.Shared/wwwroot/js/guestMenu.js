// Guest QR menu — scroll interactions run entirely client-side so the page
// never round-trips to the server while the customer scrolls. The active
// category-chip class is owned solely by this module, so Blazor re-renders
// (search / diet filtering) never fight the highlight.

let scroller = null;
let onScroll = null;
let lockUntil = 0; // ignore scroll-spy briefly during a programmatic jump

function offsetTop(el) {
	return el.getBoundingClientRect().top - scroller.getBoundingClientRect().top + scroller.scrollTop;
}

function centerChip(bar, chip) {
	bar.scrollTo({ left: Math.max(0, chip.offsetLeft - (bar.clientWidth - chip.offsetWidth) / 2), behavior: "smooth" });
}

export function init() {
	const el = document.getElementById("pbMenuScroll");
	if (!el) return;
	if (scroller && onScroll) scroller.removeEventListener("scroll", onScroll); // rebind on re-entry
	scroller = el;

	const totop = document.getElementById("pbMenuTop");

	onScroll = () => {
		if (totop) totop.classList.toggle("is-visible", scroller.scrollTop > 420);

		const bar = document.getElementById("pbMenuChips");
		if (!bar || bar.offsetParent === null) return; // chips hidden while filtering
		if (Date.now() < lockUntil) return;            // a jump is still settling

		const chips = bar.querySelectorAll(".pb-chip");
		const probe = scroller.scrollTop + 90;
		let active = chips[0];
		chips.forEach((ch) => {
			const sec = document.getElementById(ch.dataset.target);
			if (sec && offsetTop(sec) <= probe) active = ch;
		});

		let changed = false;
		chips.forEach((ch) => {
			if ((ch === active) !== ch.classList.contains("is-active")) changed = true;
			ch.classList.toggle("is-active", ch === active);
		});
		if (changed && active) centerChip(bar, active);
	};

	scroller.addEventListener("scroll", onScroll, { passive: true });
	onScroll();
}

export function jumpTo(targetId) {
	const sec = scroller && document.getElementById(targetId);
	if (!sec) return;

	lockUntil = Date.now() + 650; // freeze scroll-spy until the smooth scroll settles

	const bar = document.getElementById("pbMenuChips");
	bar?.querySelectorAll(".pb-chip").forEach((ch) => {
		const on = ch.dataset.target === targetId;
		ch.classList.toggle("is-active", on);
		if (on) centerChip(bar, ch);
	});

	scroller.scrollTo({ top: Math.max(0, offsetTop(sec) - 58), behavior: "smooth" });
}

export function scrollToTop() {
	scroller?.scrollTo({ top: 0, behavior: "smooth" });
}
