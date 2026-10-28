// =========================================================
// COOKING BABY - JAVASCRIPT ĐIỀU KHIỂN MODAL
// =========================================================

// 1. Điều khiển Modal "Đăng Chia Sẻ Công Thức Mới"
function openCreateModal() {
    var modal = document.getElementById('createRecipeModal');
    if (modal) {
        modal.classList.add('active');
    }
}

function closeCreateModal() {
    var modal = document.getElementById('createRecipeModal');
    if (modal) {
        modal.classList.remove('active');
    }
}

// 2. Điều khiển Modal "Xem Chi Tiết Món Ăn"
function openDetailModal(recipeId,title, cat, time, desc, img) {
    var modalTitle = document.getElementById('modalDetailTitle');
    var modalCat = document.getElementById('modalDetailCat');
    var modalTime = document.getElementById('modalDetailTime');
    var modalDesc = document.getElementById('modalDetailDesc');
    var modalImg = document.getElementById('modalDetailImg');
    var modal = document.getElementById('recipeDetailModal');
    var reviewRecipeId = document.getElementById('reviewRecipeId');

    if (modalTitle) modalTitle.innerText = title;
    if (modalCat) modalCat.innerText = cat;
    if (modalTime) modalTime.innerText = '⏱ ' + time + ' phút chuẩn bị';
    if (modalDesc) modalDesc.innerText = desc;
    if (modalImg) modalImg.src = img;
    if (reviewRecipeId) reviewRecipeId.value = recipeId;

    if (modal) {
        modal.classList.add('active');
    }
}

function closeDetailModal() {
    var modal = document.getElementById('recipeDetailModal');
    if (modal) {
        modal.classList.remove('active');
    }
}

// 3. Đóng Modal khi bấm chuột ra ngoài vùng nền mờ
window.addEventListener('click', function (e) {
    var createModal = document.getElementById('createRecipeModal');
    var detailModal = document.getElementById('recipeDetailModal');

    if (e.target === createModal) {
        closeCreateModal();
    }
    if (e.target === detailModal) {
        closeDetailModal();
    }
});

//4. Đánh giá sao
$(document).on("click", ".rating-star", function () {

    var selectedValue = $(this).data("value");

    $("#selectedRating").val(selectedValue);

    $(".rating-star").each(function () {

        var starValue = $(this).data("value");

        if (starValue <= selectedValue) {
            $(this).addClass("active");
        }
        else {
            $(this).removeClass("active");
        }

    });

});
