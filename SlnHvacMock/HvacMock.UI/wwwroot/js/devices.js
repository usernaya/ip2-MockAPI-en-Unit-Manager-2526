document.addEventListener("DOMContentLoaded", function () {
    const fieldSelect = document.querySelector("#fieldSelect");
    const dropdown = document.querySelector("#valueDropdown");
    const input = document.querySelector("#valueInput");

    if (!fieldSelect || !dropdown || !input) return;

    function updateValueInput() {
        const selected = fieldSelect.options[fieldSelect.selectedIndex];

        dropdown.removeAttribute("name");
        input.removeAttribute("name");

        const valuesJson = selected.getAttribute("data-values");
        const min = selected.getAttribute("data-min");
        const max = selected.getAttribute("data-max");
        const step = selected.getAttribute("data-step");

        if (valuesJson) {
            const values = JSON.parse(valuesJson);
            dropdown.innerHTML = values.map(v => `<option value="${v}">${v}</option>`).join("");
            dropdown.style.display = "block";
            input.style.display = "none";
            dropdown.setAttribute("name", "value");
        } else {
            dropdown.innerHTML = "";
            dropdown.style.display = "none";
            input.style.display = "block";
            input.setAttribute("name", "value");
            input.min = min ?? "";
            input.max = max ?? "";
            input.step = step ?? "0.5";
        }
    }

    updateValueInput();
    fieldSelect.addEventListener("change", updateValueInput);
});
