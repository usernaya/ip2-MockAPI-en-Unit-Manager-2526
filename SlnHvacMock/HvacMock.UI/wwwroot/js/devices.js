document.addEventListener("DOMContentLoaded", function () {

    const fieldSelect = document.querySelector("#fieldSelect");
    const dropdown = document.querySelector("#valueDropdown");
    const input = document.querySelector("#valueInput");

    if (!fieldSelect || !dropdown || !input) return;

    function updateValueInput() {

        const selected = fieldSelect.value;

        console.log("Field changed:", selected);

        dropdown.removeAttribute("name");
        input.removeAttribute("name");

        if (selected === "status") {
            dropdown.innerHTML = `
                <option value="on">on</option>
                <option value="off">off</option>
            `;
            dropdown.style.display = "block";
            input.style.display = "none";

            dropdown.setAttribute("name", "value"); 
        }
        else if (selected === "mode") {
            dropdown.innerHTML = `
                <option value="heating">heating</option>
                <option value="cooling">cooling</option>
                <option value="auto">auto</option>
            `;
            dropdown.style.display = "block";
            input.style.display = "none";

            dropdown.setAttribute("name", "value"); 
        }
        else {
            dropdown.innerHTML = "";
            dropdown.style.display = "none";
            input.style.display = "block";

            input.setAttribute("name", "value"); 
        }
    }

    updateValueInput();
    fieldSelect.addEventListener("change", updateValueInput);
});