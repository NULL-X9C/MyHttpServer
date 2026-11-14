const searchForm = document.querySelector('.search');
const searchInput = searchForm.querySelector('.search__input');

searchForm.addEventListener('submit', () => {
    setTimeout(() => {
        searchInput.value = '';
        searchInput.blur();
    }, 10);
});

// const SCREAMER_IMG = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTf-Re8s3GLmFtvhj-WFQaZdBimr8uDeFyOfVV5Gln0uPYeHigk2CGsi0M&s=10";
// document.addEventListener("DOMContentLoaded", () => {
//     const overlay = document.createElement("div");
//     const img = document.createElement("img");
//
//     Object.assign(overlay.style, {
//         display: "none",
//         position: "fixed",
//         top: "0", left: "0", width: "100vw", height: "100vh",
//         backgroundColor: "#000",
//         justifyContent: "center", alignItems: "center"
//     });
//
//     Object.assign(img.style, {
//         width: "600px",
//         height: "auto",
//         objectFit: "contain"
//     });
//     img.src = SCREAMER_IMG;
//
//     overlay.appendChild(img);
//     document.body.appendChild(overlay);
//
//     const targetLink = document.querySelector("#screamer-trigger");
//
//     if (targetLink) {
//         targetLink.addEventListener("click", (e) => {
//             e.preventDefault();
//
//             overlay.style.display = "flex";
//             overlay.addEventListener("click", () => {
//                 overlay.style.display = "none";
//                 audio.pause();
//             }, { once: true });
//         });
//     }
// });