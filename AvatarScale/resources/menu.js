(function() {

    /* -------------------------------
     *  CSS Embedding
     * ------------------------------- */
    function injectCSS() {
        const embeddedCSS = `
        
            // ass stands for Avatar Scale Slider
        
            /* Container styles */            
            .ass-flex-container {
                display: flex;
                justify-content: space-between;
                align-items: center;
            }

            /* ass Slider styles */
            .ass-slider-container {
                width: 100%;
            }
            .ass-slider-base {
                height: 3.25em;
                position: relative;
                background: #555;
                border-radius: 1.5em;
                cursor: pointer;
                overflow: hidden;
            }
            .ass-slider-inner {
                height: 100%;
                background: #a9a9a9;
                position: absolute;
                top: 0;
                left: 0;
            }
            .ass-snap-point {
                height: 100%;
                width: 2px;
                background: white;
                position: absolute;
                top: 0;
            }
            .ass-slider-value {
                font-size: 2em;
                position: relative;
                left: 0.5em;
                color: #fff;
                white-space: nowrap; 
            }
            
            /* Category (label) styles */
            .ass-category-label {
                font-size: 2.2em;
                position: relative;
                left: 0.5em;
                color: #fff;
                white-space: nowrap; 
                margin-right: 1em; 
            }
            
           /* Circle Button styles */
            .ass-circle-button {
                height: 2em;
                width: 2em;
                border-radius: 50%;
                background-color: #555;
                border: none;
                cursor: pointer;
                color: #fff;
                font-size: 1.75em;
                display: flex;
                align-items: center;
                justify-content: center;
                transition: background-color 0.3s;
            }
            .ass-circle-button:hover {
                background-color: #777;
            }
            
            /* Custom Toggle styles */
            .ass-custom-toggle {
                width: 5em;
                height: 2.5em;
                background-color: #555;
                border-radius: 1.25em;
                position: absolute;
                top: 50%;
                transform: translateY(-65%);
                right: 0;
                cursor: pointer;
                transition: background-color 0.3s;
            }
            
            .ass-toggle-circle {
                width: 2.5em;
                height: 2.5em;
                background-color: #a9a9a9;
                border-radius: 50%;
                position: absolute;
                top: 0;
                left: 0;
                transition: left 0.3s;
            }
            
            .ass-custom-toggle.active .ass-toggle-circle {
                left: 50%;
            }
            
            /* Label styles */
            .ass-label {
                font-size: 2em;
                white-space: nowrap;
                overflow: hidden;
                text-overflow: ellipsis;
                position: relative;
                z-index: 0;
            }
            
            .ass-toggle-setting {
                position: relative;
                height: 3.5em;
            }

        `;

        const styleElement = document.createElement('style');
        styleElement.type = 'text/css';
        styleElement.innerHTML = embeddedCSS;
        document.head.appendChild(styleElement);
    }

    /* -------------------------------
     *  Main Content Element
     * ------------------------------- */
    class MainContent {
        constructor(targetId) {
            this.element = document.createElement('div');
            this.element.id = "AvatarScaleModContainer";
            const targetElement = document.getElementById(targetId);
            if (targetElement) {
                targetElement.appendChild(this.element);
            } else {
                console.warn(`Target element "${targetId}" not found!`);
            }
        }
    }

    /* -------------------------------
    *  Generic Element Class
    * ------------------------------- */
    class Element {
        constructor(tagName, parentElement) {
            this.element = document.createElement(tagName);
            parentElement.appendChild(this.element);
        }
    }

    /* -------------------------------
     *  Container Object
     * ------------------------------- */
    class Container extends Element {
        constructor(parentElement, {
            width = '100%',
            padding = '0em',
            paddingTop = null,
            paddingRight = null,
            paddingBottom = null,
            paddingLeft = null,
            margin = '0em',
            marginTop = null,
            marginRight = null,
            marginBottom = null,
            marginLeft = null
        } = {}) {
            super('div', parentElement);
            this.element.className = "ass-container";
            this.element.style.width = width;
            this.element.style.padding = padding;
            this.element.style.margin = margin;

            // padding values
            if (paddingTop) this.element.style.paddingTop = paddingTop;
            if (paddingRight) this.element.style.paddingRight = paddingRight;
            if (paddingBottom) this.element.style.paddingBottom = paddingBottom;
            if (paddingLeft) this.element.style.paddingLeft = paddingLeft;

            // margin values
            if (marginTop) this.element.style.marginTop = marginTop;
            if (marginRight) this.element.style.marginRight = marginRight;
            if (marginBottom) this.element.style.marginBottom = marginBottom;
            if (marginLeft) this.element.style.marginLeft = marginLeft;

            this.flexContainer = new Element('div', this.element);
            this.flexContainer.element.className = "ass-flex-container";
        }

        appendElementToFlex(element) {
            this.flexContainer.element.appendChild(element);
        }

        appendElement(element) {
            this.element.appendChild(element);
        }
    }

    /* -------------------------------
    *  Category (label) Class
    * ------------------------------- */
    class Category extends Element {
        constructor(parentElement, text) {
            super('span', parentElement);
            this.element.className = "ass-category-label";
            this.element.textContent = text;
        }
    }

    /* -------------------------------
    *  Circle Button Class
    * ------------------------------- */
    class CircleButton extends Element {
        constructor(parentElement, text) {
            super('button', parentElement);
            this.element.className = "ass-circle-button";
            this.element.textContent = text;
        }
    }

    /* -------------------------------
    *  Custom Toggle Class
    * ------------------------------- */
    class CustomToggle extends Element {
        constructor(parentElement) {
            super('div', parentElement);
            this.element.className = "ass-custom-toggle";

            this.toggleCircle = new Element('div', this.element);
            this.toggleCircle.element.className = "ass-toggle-circle";
            this.state = false;

            this.element.addEventListener('click', () => {
                this.toggle();
            });
        }

        toggle() {
            this.state = !this.state;
            if (this.state) {
                this.toggleCircle.element.style.left = '50%';
            } else {
                this.toggleCircle.element.style.left = '0';
            }
        }
    }

    /* -------------------------------
    *  Label Class
    * ------------------------------- */
    class Label extends Element {
        constructor(parentElement, text) {
            super('span', parentElement);
            this.element.className = "ass-label";
            this.element.textContent = text;
        }
    }

    /* -------------------------------
    *  ToggleSetting Class
    * ------------------------------- */
    class ToggleSetting extends Element {
        constructor(parentElement, labelText) {
            super('div', parentElement);
            this.element.className = "ass-toggle-setting";

            const label = new Label(this.element, labelText);
            this.toggle = new CustomToggle(this.element);
        }
    }

    /* -------------------------------
    *  Slider Object
    * ------------------------------- */
    class Slider extends Element {
        constructor(parentElement, min = 0.1, max = 5, initialValue = 1.8) {
            super('div', parentElement);
            this.element.className = "ass-slider-container";
            this.min = min;
            this.max = max;

            // Value display
            this.valueDisplay = new Element('span', this.element);
            this.valueDisplay.element.className = "ass-slider-value";

            // Slider content
            this.sliderBase = new Element('div', this.element);
            this.sliderBase.element.className = "ass-slider-base";

            this.trackInner = new Element('div', this.sliderBase.element);
            this.trackInner.element.className = "ass-slider-inner";

            this.addEventListeners();

            this.setInitialValue(initialValue);
        }

        setInitialValue(value) {
            const percentage = (value - this.min) / (this.max - this.min);
            this.trackInner.element.style.width = `${percentage * 100}%`;
            this.valueDisplay.element.textContent = value.toFixed(2) + "m";
            this.addSnapPoint(percentage);
        }

        addEventListeners() {
            this.snapPoints = [];
            let isDragging = false;

            this.element.addEventListener('mousedown', (e) => {
                isDragging = true;
                this.updateTrackWidth(e.clientX);
            });

            window.addEventListener('mousemove', (e) => {
                if (!isDragging) return;
                this.updateTrackWidth(e.clientX);
            });

            window.addEventListener('mouseup', () => {
                isDragging = false;
            });
        }

        updateTrackWidth(clientX) {
            const rect = this.element.getBoundingClientRect();
            const paddingLeft = parseFloat(getComputedStyle(this.element).paddingLeft);
            const paddingRight = parseFloat(getComputedStyle(this.element).paddingRight);
            const effectiveWidth = rect.width - paddingLeft - paddingRight;
            let x = clientX - rect.left - paddingLeft;

            x = Math.min(Math.max(0, x), effectiveWidth);

            let percentage = x / effectiveWidth;
            const closestSnap = this.snapPoints.reduce((closest, snap) => {
                return Math.abs(closest - percentage) < Math.abs(snap - percentage) ? closest : snap;
            }, 1);

            const SNAP_TOLERANCE = 0.01;
            if (Math.abs(closestSnap - percentage) <= SNAP_TOLERANCE) {
                x = closestSnap * effectiveWidth;
                percentage = closestSnap;
            }

            this.trackInner.element.style.width = `${x}px`;

            const value = this.min + (this.max - this.min) * percentage;
            this.valueDisplay.element.textContent = value.toFixed(2) + "m";

            engine.call("asm-AvatarHeightUpdated", value);
        }

        addSnapPoint(percentage) {
            if (percentage < 0 || percentage > 1) return;
            const snap = new Element('div', this.sliderBase.element);
            snap.element.className = 'ass-snap-point';
            snap.element.style.left = `${percentage * 100}%`;
            this.snapPoints.push(percentage);
        }

        addEvenSnapPoints(count) {
            for (let i = 1; i <= count; i++) {
                this.addSnapPoint(i / (count + 1));
            }
        }
    }

    // Initialization
    injectCSS();
    const mainContent = new MainContent('btkUI-AvatarScaleMod-MainPage');
    if (mainContent.element) {

        const mainContainer = new Container(mainContent.element, {
            width: '75%',
            marginTop: '2em',
            marginLeft: '2em',
            marginBottom: '1em'
        });

        const slider = new Slider(mainContainer.flexContainer.element, 0.1, 3);
        const buttonContainer = new Container(mainContainer.flexContainer.element, {
            width: '20%',
            marginTop: '2.5em',
            marginLeft: '1em',
        });
        const circleButton1 = new CircleButton(buttonContainer.flexContainer.element, "+");
        const circleButton2 = new CircleButton(buttonContainer.flexContainer.element, "-");

        const settingsContainer = new Container(mainContent.element, {
            width: '100%',
            marginTop: '1em',
            marginLeft: '1em',
        });
        const categoryLabel = new Category(settingsContainer.element, "Universal Scaling Settings:");

        const settingsContainerInner = new Container(mainContent.element, {
            width: '90%',
            marginTop: '1em',
            marginLeft: '3em',
        });

        const toggleSetting = new ToggleSetting(settingsContainerInner.element, "Universal Scaling (Mod Network)");
        const toggleSetting2 = new ToggleSetting(settingsContainerInner.element, "Recognize Scale Gesture");
        const toggleSetting3 = new ToggleSetting(settingsContainerInner.element, "Scale Components");
    }

})();