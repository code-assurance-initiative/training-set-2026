// Building map: an inline SVG plan whose room areas can be focused and clicked.
// render() draws the plan into a host element and reports area activation (click or Enter) to .NET;
// focusArea() outlines one area.

window.roomMap = (() => {
    const hosts = new WeakMap();

    const areas = [
        { id: "area-main-hall", label: "Main hall", x: 10, y: 10, width: 180, height: 120 },
        { id: "area-workshop", label: "Workshop", x: 200, y: 10, width: 90, height: 120 },
        { id: "area-harbour-room", label: "Harbour room (first floor)", x: 10, y: 140, width: 280, height: 60 },
    ];

    function render(host, selectedAreaId, receiver) {
        const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
        svg.setAttribute("viewBox", "0 0 300 210");
        svg.setAttribute("role", "group");
        svg.setAttribute("aria-label", "Building plan");

        for (const area of areas) {
            const rect = document.createElementNS("http://www.w3.org/2000/svg", "rect");
            rect.setAttribute("x", area.x);
            rect.setAttribute("y", area.y);
            rect.setAttribute("width", area.width);
            rect.setAttribute("height", area.height);
            rect.setAttribute("class", "map-area");
            rect.setAttribute("role", "link");
            rect.setAttribute("tabindex", "0");
            rect.setAttribute("aria-label", area.label);
            rect.dataset.areaId = area.id;
            const title = document.createElementNS("http://www.w3.org/2000/svg", "title");
            title.textContent = area.label;
            rect.appendChild(title);
            svg.appendChild(rect);
        }

        const select = (event) => {
            const id = event.target?.dataset?.areaId;
            if (id) {
                receiver.invokeMethodAsync("OnAreaSelected", id);
            }
        };
        const onClick = (event) => select(event);
        const onKeyDown = (event) => {
            if (event.key === "Enter") {
                select(event);
            }
        };
        svg.addEventListener("click", onClick);
        svg.addEventListener("keydown", onKeyDown);

        host.replaceChildren(svg);
        hosts.set(host, { svg, onClick, onKeyDown });
        focusArea(host, selectedAreaId);
    }

    function focusArea(host, areaId) {
        const state = hosts.get(host);
        if (!state) {
            return;
        }
        for (const rect of state.svg.querySelectorAll(".map-area")) {
            rect.classList.toggle("map-area--selected", rect.dataset.areaId === areaId);
        }
    }

    function dispose(host) {
        const state = hosts.get(host);
        if (state) {
            state.svg.removeEventListener("click", state.onClick);
            state.svg.removeEventListener("keydown", state.onKeyDown);
            hosts.delete(host);
        }
    }

    return { render, focusArea, dispose };
})();
