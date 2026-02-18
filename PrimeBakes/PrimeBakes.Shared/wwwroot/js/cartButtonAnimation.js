/**
 * Animated cart button - GSAP-based animation for the "Go to Cart" button.
 * Implements the full interactive cart button animation with:
 *   - SVG morph path interpolation (wave effect at button top)
 *   - Cupcake frosting puff morph (frosting expands then settles)
 *   - Item fly-up, hover, drop into cart sequence
 *   - Cart bump, checkmark tick, roll-out and roll-back
 */
window.CartButtonAnimation = {
    /**
     * Linearly interpolates between two SVG path strings that share the same command structure.
     * Extracts all numeric values from both paths and blends them by factor t (0–1).
     */
    _lerpPath: function (pathA, pathB, t) {
        var numsA = pathA.match(/-?\d+\.?\d*/g).map(Number);
        var numsB = pathB.match(/-?\d+\.?\d*/g).map(Number);
        if (numsA.length !== numsB.length) return t < 0.5 ? pathA : pathB;
        var idx = 0;
        return pathA.replace(/-?\d+\.?\d*/g, function () {
            var val = numsA[idx] + (numsB[idx] - numsA[idx]) * t;
            idx++;
            return Math.round(val * 10000) / 10000;
        });
    },

    // ---- Cupcake shape paths (all share identical command structure for interpolation) ----
    // Structure: M C C L L L Z  (18 numbers each)
    // The cupcake: frosting dome on top, tapered wrapper on bottom

    // Resting shape: normal cupcake
    ITEM_REST:  'M5 13C5 13 4 9 4 8C4 5 7 3 12 3C17 3 20 5 20 8C20 9 19 13 19 13L17 22L7 22L5 13Z',

    // Puffed shape: frosting expands outward, wider and taller
    ITEM_PUFF:  'M3 13C3 13 2 8 2 6C2 2 6 0 12 0C18 0 22 2 22 6C22 8 21 13 21 13L17 22L7 22L3 13Z',

    // Settled shape: slightly puffed, elastic resting
    ITEM_SETTLE:'M4 13C4 13 3 9 3 7C3 4 7 2 12 2C17 2 21 4 21 7C21 9 20 13 20 13L17 22L7 22L4 13Z',

    /**
     * Plays the add-to-cart animation on the specified button element.
     * @param {string} buttonId - The DOM id of the button element.
     * @returns {Promise} Resolves when the animation completes.
     */
    play: function (buttonId) {
        return new Promise(function (resolve) {
            var button = document.getElementById(buttonId);
            if (!button || button.classList.contains('animating')) {
                resolve();
                return;
            }

            button.classList.add('animating');

            var morphPath = button.querySelector('.morph path');
            var itemPaths = button.querySelectorAll('.cupcake svg > path:first-child');
            var self = CartButtonAnimation;

            var MORPH_FLAT = 'M0 12C6 12 17 12 32 12C47.9024 12 58 12 64 12V13H0V12Z';
            var MORPH_WAVE = 'M0 12C6 12 20 10 32 0C43.9024 9.99999 58 12 64 12V13H0V12Z';

            function setItemPath(d) {
                itemPaths.forEach(function (p) { p.setAttribute('d', d); });
            }

            // --- 1. Background press/bounce ---
            gsap.to(button, {
                keyframes: [{
                    '--background-scale': .97,
                    duration: .15
                }, {
                    '--background-scale': 1,
                    delay: .125,
                    duration: 1.2,
                    ease: 'elastic.out(1, .6)'
                }]
            });

            // --- 2. Cupcake position: rise, hover, drop, disappear ---
            gsap.to(button, {
                keyframes: [{
                    '--item-scale': 1,
                    '--item-y': '-42px',
                    '--cart-x': '0px',
                    '--cart-scale': 1,
                    duration: .4,
                    ease: 'power1.in'
                }, {
                    '--item-y': '-40px',
                    duration: .3
                }, {
                    '--item-y': '16px',
                    '--item-scale': .9,
                    duration: .25,
                    ease: 'none'
                }, {
                    '--item-scale': 0,
                    duration: .3,
                    ease: 'none'
                }]
            });

            // --- 3. Cupcake SVG morph: resting → puffed (frosting expands) → settled (elastic) → reset ---
            if (itemPaths.length > 0) {
                var m1 = { t: 0 }, m2 = { t: 0 };
                gsap.timeline()
                    // Phase 1: resting → puffed frosting
                    .to(m1, {
                        t: 1,
                        duration: .25,
                        delay: .25,
                        onUpdate: function () {
                            setItemPath(self._lerpPath(self.ITEM_REST, self.ITEM_PUFF, m1.t));
                        }
                    })
                    // Phase 2: puffed → settled (elastic bounce)
                    .to(m2, {
                        t: 1,
                        duration: .85,
                        ease: 'elastic.out(1, .5)',
                        onUpdate: function () {
                            setItemPath(self._lerpPath(self.ITEM_PUFF, self.ITEM_SETTLE, m2.t));
                        }
                    })
                    // Phase 3: reset back to resting shape (cupcake is hidden by then)
                    .call(function () {
                        setItemPath(self.ITEM_REST);
                    }, null, '+=1.25');
            }

            // --- 4. Second cupcake layer reveal (item inside cart) ---
            gsap.to(button, {
                '--item-second-y': '0px',
                delay: .835,
                duration: .12
            });

            // --- 5. Morph path wave (ripple at button top when item pops out) ---
            if (morphPath) {
                var morphObj = { t: 0 };
                gsap.timeline()
                    .to(morphObj, {
                        t: 1,
                        duration: .25,
                        ease: 'power1.out',
                        onUpdate: function () {
                            morphPath.setAttribute('d', self._lerpPath(MORPH_FLAT, MORPH_WAVE, morphObj.t));
                        }
                    })
                    .to(morphObj, {
                        t: 0,
                        duration: .15,
                        ease: 'none',
                        onUpdate: function () {
                            morphPath.setAttribute('d', self._lerpPath(MORPH_FLAT, MORPH_WAVE, morphObj.t));
                        }
                    });
            }

            // --- 6. Cart clip, bump, tick, roll out, roll back with text ---
            gsap.to(button, {
                keyframes: [{
                    '--cart-clip': '12px',
                    '--cart-clip-x': '3px',
                    delay: .9,
                    duration: .06
                }, {
                    '--cart-y': '2px',
                    duration: .1
                }, {
                    '--cart-tick-offset': '0px',
                    '--cart-y': '0px',
                    duration: .2,
                    onComplete: function () {
                        button.style.overflow = 'hidden';
                    }
                }, {
                    '--cart-x': '52px',
                    '--cart-rotate': '-15deg',
                    duration: .2
                }, {
                    '--cart-x': '104px',
                    '--cart-rotate': '0deg',
                    duration: .2,
                    clearProps: true,
                    onComplete: function () {
                        button.style.overflow = 'hidden';
                        button.style.setProperty('--text-o', 0);
                        button.style.setProperty('--text-x', '0px');
                        button.style.setProperty('--cart-x', '-104px');
                    }
                }, {
                    '--text-o': 1,
                    '--text-x': '12px',
                    '--cart-x': '-48px',
                    '--cart-scale': .75,
                    duration: .25,
                    clearProps: true,
                    onComplete: function () {
                        button.classList.remove('animating');
                        // Reset morph path to flat
                        if (morphPath) morphPath.setAttribute('d', MORPH_FLAT);
                        setTimeout(function () { resolve(); }, 400);
                    }
                }]
            });

            // --- 7. Fade out text and badge while animation plays ---
            gsap.to(button, {
                keyframes: [{
                    '--text-o': 0,
                    duration: .3
                }]
            });
        });
    }
};
