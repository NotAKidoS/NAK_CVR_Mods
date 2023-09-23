(function() {
    /* -------------------------------
     *  CSS Embedding
     * ------------------------------- */
    {
        const embeddedCSS = `
            .slider-container {
                width: 80%;
                padding-left: 2em;
                position: relative; 
            }
        
            .avatar-scale-slider-container {
                width: 100%;
                height: 3em;
                position: relative;
                background: #555;
                border-radius: 1.5em;
                cursor: pointer;
                overflow: hidden;
            }

            .avatar-scale-track-inner {
                height: 100%;
                background: #a9a9a9;
                position: absolute;
                top: 0;
                left: 0;
            }

            .avatar-scale-snap-point {
                height: 100%;
                width: 2px;
                background: white;
                position: absolute;
                top: 0;
            }
            
            .slider-display-value {
                font-size: 2em;
                position: relative;
                left: 0.5em;
                color: #fff;
                white-space: nowrap; 
            }
            
            .lock-icon {
                position: absolute;
                top: 50%;
                right: 1em;
                transform: translateY(-50%);
                width: 1.5em;
                height: 1.5em;
                background: url('path_to_lock_icon.png') no-repeat center;
                background-size: contain;
            }
        `;

        const styleElement = document.createElement('style');
        styleElement.type = 'text/css';
        styleElement.innerHTML = embeddedCSS;
        document.head.appendChild(styleElement);
    }

    /* -------------------------------
     *  Content Injection
     * ------------------------------- */
    {
        const contentBlock = document.createElement('div');
        contentBlock.innerHTML = `
            <div class="settings-subcategory">
                <div class="subcategory-name">Avatar Motion Tweaker</div>
                <div class="subcategory-description"></div>
            </div>

            <div class="row-wrapper">
                <div class="option-caption">Crouch limit: </div>
                <div class="option-input">
                    <div id="CrouchLimit" class="inp_slider no-scroll" data-min="0" data-max="100" data-current="75"></div>
                </div>
            </div>
            
            <div class="slider-container">
                <div class="slider-display-value">
                    <span class="slider-value">0m</span>
                </div>
                <div class="avatar-scale-slider-container">
                    <div class="avatar-scale-track-inner"></div>
                </div>
                <div class="lock-icon"></div>
            </div>
        `;

        const targetElement = document.getElementById('btkUI-AvatarScaleMod-MainPage');
        if(targetElement) {
            targetElement.appendChild(contentBlock);
        } else {
            console.warn('Target element "btkUI-AvatarScaleMod-MainPage" not found!');
        }
    }

    /* -------------------------------
     *  Event Handlers & Utility Functions
     * ------------------------------- */
    {
        const sliderContainer = document.querySelector('.avatar-scale-slider-container');
        const trackInner = document.querySelector('.avatar-scale-track-inner');
        const valueDisplay = document.querySelector('.slider-value');

        const SNAP_TOLERANCE = 0.02;
        let snapPoints = [];

        let isDragging = false;

        sliderContainer.addEventListener('mousedown', (e) => {
            isDragging = true;
            updateTrackWidth(e.clientX);
        });

        window.addEventListener('mousemove', (e) => {
            if (!isDragging) return;
            updateTrackWidth(e.clientX);
        });

        window.addEventListener('mouseup', () => {
            isDragging = false;
        });

        function updateTrackWidth(clientX) {
            const rect = sliderContainer.getBoundingClientRect();

            // Get padding values from the slider container
            const paddingLeft = parseFloat(getComputedStyle(sliderContainer).paddingLeft);
            const paddingRight = parseFloat(getComputedStyle(sliderContainer).paddingRight);

            // Calculate the effective width and position based on padding
            const effectiveWidth = rect.width - paddingLeft - paddingRight;
            let x = clientX - rect.left - paddingLeft;

            // Ensure the position is within the bounds of the effective width
            x = Math.min(Math.max(0, x), effectiveWidth);

            const percentage = x / effectiveWidth;
            const closestSnap = snapPoints.reduce((closest, snap) => {
                return Math.abs(closest - percentage) < Math.abs(snap - percentage) ? closest : snap;
            }, 1);

            if (Math.abs(closestSnap - percentage) <= SNAP_TOLERANCE) {
                x = closestSnap * effectiveWidth;
            }

            trackInner.style.width = `${x}px`;
            valueDisplay.textContent = (x / effectiveWidth * 100).toFixed(2) + "m";
        }

        function addSnapPoint(percentage) {
            if (percentage < 0 || percentage > 1) return;
            const snap = document.createElement('div');
            snap.className = 'avatar-scale-snap-point';
            snap.style.left = `${percentage * 100}%`;
            sliderContainer.appendChild(snap);
            snapPoints.push(percentage);
        }

        // To evenly space out snap points:
        function addEvenSnapPoints(count) {
            for (let i = 1; i <= count; i++) {
                addSnapPoint(i / (count + 1));
            }
        }

        // Example usage:
        addEvenSnapPoints(5);  // Adds 5 evenly spaced snap points
    }
})();