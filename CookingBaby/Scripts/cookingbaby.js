/**
 * Cooking Baby - Bếp Vui Mỗi Ngày
 * Tập tin JavaScript chính: Scripts/cookingbaby.js
 * Phiên bản tách triệt để JavaScript khỏi View (Strict JS Separation)
 */

// ==========================================================================
// 1. COMMON & UI HELPERS (Thông báo Toast, Modal xác nhận, Khởi tạo chung)
// ==========================================================================

/**
 * Tự động gắn AntiForgeryToken vào mọi request AJAX POST nội bộ
 */
var cookingBabyToastTimer;

function showCookingBabyToast(message, type) {
    var toast = document.getElementById('cookingBabyToast');
    if (!toast) {
        toast = document.createElement('div');
        toast.id = 'cookingBabyToast';
        toast.className = 'cb-toast';
        toast.setAttribute('role', 'status');
        toast.setAttribute('aria-live', 'polite');
        toast.innerHTML = '<span class="cb-toast-icon" aria-hidden="true"></span><span class="cb-toast-message"></span><button type="button" class="cb-toast-close" aria-label="Đóng thông báo">×</button><span class="cb-toast-progress"></span>';
        document.body.appendChild(toast);
        toast.querySelector('.cb-toast-close').addEventListener('click', function () { toast.classList.remove('is-visible'); });
    }
    window.clearTimeout(cookingBabyToastTimer);
    toast.className = 'cb-toast cb-toast-' + (type || 'info');
    toast.querySelector('.cb-toast-icon').textContent = type === 'success' ? '✓' : (type === 'error' ? '!' : 'i');
    toast.querySelector('.cb-toast-message').textContent = message || '';
    toast.classList.add('is-visible');
    cookingBabyToastTimer = window.setTimeout(function () { toast.classList.remove('is-visible'); }, 4200);
}

$.ajaxPrefilter(function (options) {
    if ((options.type || '').toUpperCase() !== 'POST') return;
    if (/^https?:\/\//i.test(options.url) && new URL(options.url, window.location.href).origin !== window.location.origin) return;

    var token = $('#ajaxAntiForgery input[name="__RequestVerificationToken"]').val() ||
                $('input[name="__RequestVerificationToken"]').val();
    if (!token) return;

    if (typeof options.data === 'string') {
        if (options.data.indexOf('__RequestVerificationToken') === -1) {
            options.data += (options.data ? '&' : '') + '__RequestVerificationToken=' + encodeURIComponent(token);
        }
    } else if (typeof options.data === 'object' && options.data !== null) {
        options.data.__RequestVerificationToken = token;
    } else if (!options.data) {
        options.data = { __RequestVerificationToken: token };
    }
});

/**
 * Tự động highlight Tab điều hướng (Active Nav Tabs) dựa trên URL hiện tại
 */
function initNavigationTabs() {
    var tabs = document.querySelectorAll(".nav-tab-item");
    if (!tabs || tabs.length === 0) return;

    var currentPath = window.location.pathname.toLowerCase();

    tabs.forEach(function (tab) {
        tab.classList.remove("active");
    });

    tabs.forEach(function (tab) {
        try {
            var tabPath = new URL(tab.href, window.location.origin).pathname.toLowerCase();
            if ((currentPath === "/" || currentPath === "/home" || currentPath === "/home/index")
                && (tabPath === "/" || tabPath === "/home" || tabPath === "/home/index")) {
                tab.classList.add("active");
            } else if (currentPath === tabPath && currentPath !== "/") {
                tab.classList.add("active");
            }
        } catch (e) {
            // bỏ qua URL không hợp lệ
        }
    });
}

/**
 * Điều khiển mở/đóng Modal Đăng Công Thức Mới
 */
function openCreateModal() {
    var modal = document.getElementById('createRecipeModal');
    if (modal) {
        $(modal).css('display', 'flex').addClass('active');
    }
}

function closeCreateModal() {
    var modal = document.getElementById('createRecipeModal');
    if (modal) {
        $(modal).css('display', 'none').removeClass('active');
    }
}

/**
 * Điều khiển mở/đóng Modal Chi Tiết Món Ăn
 */
var currentModalRecipeId = 0;

function openDetailModal(recipeId) {
    currentModalRecipeId = recipeId;

    $.ajax({
        url: '/Recipe/GetRecipeDetail',
        type: 'GET',
        data: { id: recipeId },
        success: function (res) {
            if (!res || !res.success) {
                alert('Không tìm thấy thông tin công thức.');
                return;
            }

            $('#reviewRecipeId').val(recipeId);
            $('#modalDetailTitle').text(res.title || '');
            $('#modalDetailCat').text(res.categoryName || 'Món ngon');
            $('#modalDetailTime').text('⏱ ' + (res.cookTime || 0) + ' phút');
            $('#modalDetailDesc').text(res.description || '');
            $('#modalDetailAuthor').text(res.authorName || 'Không xác định');

            // 1. Nút Lưu trong Modal
            var saveBtn = $('#modalToggleSaveBtn');
            if (res.isSaved) {
                saveBtn.addClass('saved').css('background', '#E53E3E').text('♥ Đã lưu trong sổ tay');
            } else {
                saveBtn.removeClass('saved').css('background', '#FF5A1F').text('♡ Lưu vào sổ tay');
            }

            // 2. Quyền tác giả: hiển thị nút Sửa/Xóa
            if (res.authorId === res.currentUserId) {
                $('#modalAuthorActions').css('display', 'flex');
                $('#modalAuthorPermission').hide();
                $('#modalEditBtn').attr('href', '/Recipe/EditRecipe/' + res.recipeId);
                $('#modalDeleteId').val(res.recipeId);
            } else {
                $('#modalAuthorActions').hide();
                $('#modalAuthorPermission').show();
            }

            // 3. Ảnh món ăn
            var imageName = res.imageUrl || 'pancake.jpg';
            var finalSrc = '';
            if (imageName.indexOf('/') === 0 || imageName.indexOf('http') === 0) {
                finalSrc = imageName;
            } else if (imageName.indexOf('Uploads/') === 0) {
                finalSrc = '/' + imageName;
            } else {
                finalSrc = '/Content/images/' + imageName;
            }
            $('#modalDetailImg').attr('src', finalSrc);

            updateRatingSummary(res.avgRating || 0, res.reviewCount || 0);
            renderIngredients(res.ingredients || []);
            renderSteps(res.steps || []);
            resetReviewForm();
            loadReviews(recipeId);

            $('#recipeDetailModal').css('display', 'flex').addClass('active');
        },
        error: function () {
            alert('Không thể tải chi tiết công thức.');
        }
    });
}

function closeDetailModal() {
    $('#recipeDetailModal').css('display', 'none').removeClass('active');
}

/**
 * Khởi tạo biểu đồ Admin Chart.js an toàn từ thuộc tính data-*
 */
function initAdminCharts() {
    if (typeof Chart === 'undefined') return;

    // Biểu đồ 1: Phân bố theo danh mục
    var catCanvas = document.getElementById('categoryChart');
    if (catCanvas && !catCanvas.chartInstance) {
        try {
            var rawLabels = catCanvas.getAttribute('data-labels') || '[]';
            var rawValues = catCanvas.getAttribute('data-values') || '[]';
            var catLabels = JSON.parse(rawLabels);
            var catData = JSON.parse(rawValues);

            catCanvas.chartInstance = new Chart(catCanvas.getContext('2d'), {
                type: 'doughnut',
                data: {
                    labels: catLabels,
                    datasets: [{
                        data: catData,
                        backgroundColor: [
                            '#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF', '#FF9F40'
                        ],
                        borderWidth: 2,
                        borderColor: '#FFFFFF'
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: 'bottom',
                            labels: { boxWidth: 14, font: { size: 12 } }
                        }
                    }
                }
            });
        } catch (err) {
            console.error("Lỗi vẽ biểu đồ danh mục:", err);
        }
    }

    // Biểu đồ 2: Xu hướng đăng bài theo tháng
    var monCanvas = document.getElementById('monthlyChart');
    if (monCanvas && !monCanvas.chartInstance) {
        try {
            var rawMonLabels = monCanvas.getAttribute('data-labels') || '[]';
            var rawMonValues = monCanvas.getAttribute('data-values') || '[]';
            var monLabels = JSON.parse(rawMonLabels);
            var monData = JSON.parse(rawMonValues);

            monCanvas.chartInstance = new Chart(monCanvas.getContext('2d'), {
                type: 'bar',
                data: {
                    labels: monLabels,
                    datasets: [{
                        label: 'Số công thức đăng',
                        data: monData,
                        backgroundColor: '#FF5A1F',
                        borderRadius: 6,
                        barThickness: 28
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    scales: {
                        y: {
                            beginAtZero: true,
                            ticks: { precision: 0 }
                        }
                    },
                    plugins: {
                        legend: { display: false }
                    }
                }
            });
        } catch (err) {
            console.error("Lỗi vẽ biểu đồ tháng:", err);
        }
    }
}

// Bắt sự kiện bàn phím ESC và click ra ngoài overlay để đóng modal
document.addEventListener('DOMContentLoaded', function () {
    initNavigationTabs();
    initAdminCharts();
});

document.addEventListener('click', function (e) {
    var createModal = document.getElementById('createRecipeModal');
    if (createModal && e.target === createModal) {
        closeCreateModal();
    }
    var detailModal = document.getElementById('recipeDetailModal');
    if (detailModal && e.target === detailModal) {
        closeDetailModal();
    }
    if (e.target && e.target.classList.contains('btn-cancel-custom')) {
        closeCreateModal();
    }
});

document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') {
        closeCreateModal();
        closeDetailModal();
    }
});


// ==========================================================================
// 2. SEARCH & CATEGORY FILTER (Xử lý lọc, tìm kiếm ở Trang chủ / Khám phá)
// ==========================================================================

/**
 * Chuyển tab danh mục mà vẫn bảo lưu các tags nguyên liệu, từ khóa và tiêu chí sắp xếp
 */
function filterByCategory(catId) {
    var form = document.getElementById('filterForm');
    if (!form) return;

    var catInput = form.querySelector('input[name="categoryId"]');
    if (catId === '' || catId === null || catId === undefined) {
        if (catInput) catInput.remove();
    } else {
        if (!catInput) {
            catInput = document.createElement('input');
            catInput.type = 'hidden';
            catInput.name = 'categoryId';
            form.appendChild(catInput);
        }
        catInput.value = catId;
    }
    form.submit();
}

// Tự động submit khi chọn tiêu chí sắp xếp
$(document).on('change', '#sortSelect', function () {
    var hiddenSort = document.getElementById('hiddenSortBy');
    if (hiddenSort) hiddenSort.value = this.value;
    var form = document.getElementById('filterForm');
    if (form) form.submit();
});


// ==========================================================================
// 3. SAVED RECIPES / BOOKMARK (Nút thả tim / Lưu món ăn AJAX)
// ==========================================================================

/**
 * Thả tim / Lưu món ăn dùng chung cho mọi thẻ card
 */
function toggleSaved(recipeId, btn, isSavedPage, targetUrl) {
    if (!recipeId) return;
    var requestUrl = targetUrl || $(btn).data('url') || '/Home/ToggleSaved';

    $.ajax({
        url: requestUrl,
        type: 'POST',
        data: { recipeId: recipeId, id: recipeId },
        success: function (res) {
            if (!res.success) {
                if (res.requireLogin) {
                    if (confirm(res.message + "\n\nBạn có muốn chuyển đến trang Đăng nhập ngay bây giờ không?")) {
                        window.location.href = '/Account/Login';
                    }
                    return;
                }
                alert(res.message || 'Không thể thực hiện thao tác.');
                return;
            }

            if (res.isSaved) {
                $(btn).addClass('saved active').attr('title', 'Bỏ lưu món này');
                $(btn).text('♥');
            } else {
                $(btn).removeClass('saved active').attr('title', 'Lưu món này');
                $(btn).text('♡');
            }

            // Nếu đang ở trang Sổ tay lưu trữ thì tự động ẩn thẻ vừa bỏ lưu
            if (isSavedPage && !res.isSaved) {
                $('#recipe-card-' + recipeId).fadeOut(300, function () {
                    $(this).remove();
                    if ($('.saved-card').length === 0 || $('#savedRecipeGrid .saved-card').length === 0) {
                        location.reload();
                    }
                });
            }

            // Cập nhật số đếm trên tab Sổ Tay Đã Lưu
            var badge = $('.nav-tab-item[href*="SavedRecipes"] .nav-badge');
            if (badge.length) {
                var count = parseInt(badge.text(), 10) || 0;
                badge.text(res.isSaved ? count + 1 : Math.max(0, count - 1));
            }
        },
        error: function (xhr, status, err) {
            console.error("Lỗi ToggleSaved:", status, err, xhr.responseText);
            alert('Không thể thực hiện thao tác Lưu/Bỏ lưu (Lỗi kết nối máy chủ).');
        }
    });
}

/**
 * Lưu / Bỏ lưu trực tiếp ngay trong Modal chi tiết món ăn
 */
function toggleSavedFromModal() {
    if (!currentModalRecipeId) return;
    var btn = $('#modalToggleSaveBtn');

    toggleSaved(currentModalRecipeId, btn, false);

    setTimeout(function () {
        if (btn.hasClass('saved') || btn.text().indexOf('Đã lưu') >= 0) {
            btn.css('background', '#E53E3E').text('♥ Đã lưu trong sổ tay');
        } else {
            btn.css('background', '#FF5A1F').text('♡ Lưu vào sổ tay');
        }
    }, 200);
}

/**
 * Bỏ lưu công thức từ trang Sổ tay lưu trữ (SavedRecipes.cshtml)
 */
function handleUnsave(recipeId, targetUrl) {
    if (!confirm("Bạn có chắc muốn bỏ lưu công thức này khỏi sổ tay?")) {
        return;
    }

    var requestUrl = targetUrl || '/Home/ToggleSaved';

    $.ajax({
        url: requestUrl,
        type: 'POST',
        data: { recipeId: recipeId, id: recipeId },
        success: function (res) {
            if (res && res.success) {
                $('#recipe-card-' + recipeId).fadeOut(350, function () {
                    $(this).remove();

                    // Cập nhật số lượng trên badge Header
                    var badge = $('.nav-tab-item[href*="SavedRecipes"] .nav-badge');
                    if (badge.length) {
                        var count = parseInt(badge.text(), 10) || 0;
                        badge.text(Math.max(0, count - 1));
                    }

                    // Nếu không còn thẻ nào, reload để hiện khung Sổ tay trống
                    if ($('#savedRecipeGrid .saved-card').length === 0) {
                        location.reload();
                    }
                });
            } else {
                alert((res && res.message) ? res.message : 'Không thể cập nhật trạng thái lưu.');
            }
        },
        error: function (xhr, status, error) {
            console.error("Lỗi bỏ lưu:", status, error, xhr.responseText);
            alert('Có lỗi kết nối đến máy chủ khi bỏ lưu món ăn.');
        }
    });
}

/**
 * Lưu / Bỏ lưu từ trang Chi tiết độc lập (/Recipe/Details/{id})
 */
function toggleSaveOnPage(recipeId, targetUrl) {
    var btn = document.getElementById('pageBtnSave');
    var requestUrl = targetUrl || (btn ? btn.getAttribute('data-url') : null) || '/Recipe/ToggleSaved';

    $.ajax({
        url: requestUrl,
        type: 'POST',
        data: { id: recipeId, recipeId: recipeId },
        success: function (res) {
            if (res && res.success) {
                var icon = document.getElementById('pageSaveIcon');
                if (btn) {
                    if (res.isSaved) {
                        btn.classList.add('active');
                        if (icon) icon.textContent = '♥ Đã Lưu';
                    } else {
                        btn.classList.remove('active');
                        if (icon) icon.textContent = '♡ Lưu Món';
                    }
                }
            } else if (res && res.message) {
                alert(res.message);
            }
        },
        error: function () {
            alert('Không thể thực hiện lưu món. Vui lòng thử lại!');
        }
    });
}

// Bắt sự kiện click nút bỏ lưu bằng data-attributes
$(document).on('click', '.btn-heart-unsave', function (e) {
    e.stopPropagation();
    var recipeId = $(this).data('recipe-id') || $(this).attr('data-recipe-id');
    var url = $(this).data('url') || $(this).attr('data-url') || '/Home/ToggleSaved';
    handleUnsave(recipeId, url);
});


// ==========================================================================
// 4. RECIPE FORM DYNAMIC FIELDS (Thêm/Xóa dòng Nguyên liệu, Bước làm, Xem trước ảnh)
// ==========================================================================

/**
 * Xem trước ảnh đại diện khi người dùng chọn file
 */
function previewSelectedImage(input, previewId) {
    if (input.files && input.files[0]) {
        var file = input.files[0];
        var ext = file.name.split('.').pop().toLowerCase();
        if (['jpg', 'jpeg', 'png', 'webp', 'gif'].indexOf(ext) === -1) {
            alert('Chỉ chấp nhận các định dạng ảnh: .jpg, .jpeg, .png, .webp, .gif');
            input.value = '';
            return;
        }
        if (file.size > 5 * 1024 * 1024) {
            alert('Dung lượng ảnh không được vượt quá 5MB.');
            input.value = '';
            return;
        }
        var reader = new FileReader();
        reader.onload = function (e) {
            var img = document.getElementById(previewId);
            if (img) img.src = e.target.result;
        };
        reader.readAsDataURL(file);
    }
}

/**
 * Xem trước ảnh trong Modal Đăng Công Thức
 */
function previewCreateModalImage(input) {
    if (input.files && input.files[0]) {
        var file = input.files[0];
        var ext = file.name.split('.').pop().toLowerCase();
        if (['jpg', 'jpeg', 'png', 'webp', 'gif'].indexOf(ext) === -1) {
            alert('Chỉ chấp nhận các file ảnh: .jpg, .jpeg, .png, .webp, .gif');
            input.value = '';
            $('#createPreviewBox').hide();
            return;
        }
        if (file.size > 5 * 1024 * 1024) {
            alert('Dung lượng ảnh tối đa 5MB.');
            input.value = '';
            $('#createPreviewBox').hide();
            return;
        }
        var reader = new FileReader();
        reader.onload = function (e) {
            $('#createImagePreview').attr('src', e.target.result);
            $('#createPreviewBox').removeClass('display-none').css('display', 'flex');
        };
        reader.readAsDataURL(file);
    } else {
        $('#createPreviewBox').hide();
    }
}

/**
 * Kiểm tra tính hợp lệ của dữ liệu trước khi gửi Modal Tạo Công Thức
 */
function validateCreateModalForm() {
    var titleInput = document.getElementById("createTitle");
    var cookTimeInput = document.getElementById("createCookTime");
    var ingredientsInput = document.getElementById("createIngredients");
    var instructionsInput = document.getElementById("createInstructions");
    var imageFileInput = document.getElementById("createImageFile");

    var errors = [];

    if (!titleInput || titleInput.value.trim().length < 2) {
        errors.push("Tên món ăn phải có ít nhất 2 ký tự.");
    }

    if (cookTimeInput) {
        var cookTimeVal = parseInt(cookTimeInput.value, 10);
        if (isNaN(cookTimeVal) || cookTimeVal <= 0 || cookTimeVal > 1440) {
            errors.push("Thời gian nấu phải hợp lệ (từ 1 đến 1440 phút).");
        }
    }

    if (ingredientsInput) {
        var ingList = ingredientsInput.value.trim().split(/[,;]/).filter(function (x) {
            return x.trim().length > 0;
        });
        if (ingList.length === 0) {
            errors.push("Vui lòng nhập ít nhất 1 nguyên liệu.");
        }
    }

    if (instructionsInput) {
        var stepList = instructionsInput.value.trim().split(/\r?\n/).filter(function (x) {
            return x.trim().length > 0;
        });
        if (stepList.length === 0) {
            errors.push("Vui lòng nhập các bước thực hiện nấu ăn.");
        }
    }

    if (imageFileInput && imageFileInput.files.length > 0) {
        var fileName = imageFileInput.files[0].name.toLowerCase();
        if (!/\.(jpg|jpeg|png|webp|gif)$/.test(fileName)) {
            errors.push("File ảnh đại diện chỉ chấp nhận định dạng .jpg, .jpeg, .png hoặc .webp.");
        }
    }

    if (errors.length > 0) {
        alert("⚠️ Vui lòng kiểm tra lại thông tin:\n\n- " + errors.join("\n- "));
        return false;
    }

    return true;
}

/**
 * Kiểm tra tính hợp lệ của Form tạo trang độc lập (/Recipe/Create)
 */
function validateCreatePageForm() {
    var title = (document.getElementById('pageTitle').value || '').trim();
    var cookTime = parseInt(document.getElementById('pageCookTime').value, 10);
    var desc = (document.getElementById('pageDescription').value || '').trim();
    var ingr = (document.getElementById('pageIngredients').value || '').trim();
    var inst = (document.getElementById('pageInstructions').value || '').trim();

    var errBox = document.getElementById('createClientError');
    if (errBox) {
        errBox.style.display = 'none';
        errBox.textContent = '';
    }

    if (!title) {
        showError('Vui lòng nhập tên món ăn!');
        return false;
    }
    if (isNaN(cookTime) || cookTime <= 0) {
        showError('Thời gian nấu phải lớn hơn 0 phút!');
        return false;
    }
    if (!desc) {
        showError('Vui lòng nhập mô tả món ăn!');
        return false;
    }
    if (!ingr) {
        showError('Vui lòng nhập ít nhất một nguyên liệu!');
        return false;
    }
    if (!inst) {
        showError('Vui lòng nhập các bước thực hiện!');
        return false;
    }
    return true;

    function showError(msg) {
        if (errBox) {
            errBox.textContent = msg;
            errBox.style.display = 'block';
            errBox.scrollIntoView({ behavior: 'smooth', block: 'center' });
        } else {
            alert(msg);
        }
    }
}

/**
 * Kiểm tra tính hợp lệ của Form Chỉnh Sửa (/Recipe/EditRecipe)
 */
function validateEditForm() {
    var title = (document.getElementById("editTitle").value || '').trim();
    var cookTime = parseInt(document.getElementById("editCookTime").value, 10);
    var desc = (document.getElementById("editDescription").value || '').trim();
    var ings = (document.getElementById("editIngredients").value || '').trim();
    var steps = (document.getElementById("editInstructions").value || '').trim();
    var fileInput = document.getElementById("editImageFile");
    var errBox = document.getElementById("editClientError");

    var errors = [];

    if (title.length < 2) {
        errors.push("Tên món ăn phải có ít nhất 2 ký tự.");
    }
    if (isNaN(cookTime) || cookTime <= 0 || cookTime > 1440) {
        errors.push("Thời gian nấu phải là số nguyên dương từ 1 đến 1440 phút.");
    }
    if (desc.length < 5) {
        errors.push("Mô tả ngắn món ăn cần tối thiểu 5 ký tự.");
    }
    var ingItems = ings.split(/[,;]/).map(function (s) { return s.trim(); }).filter(function (s) { return s.length > 0; });
    if (ingItems.length === 0) {
        errors.push("Vui lòng nhập ít nhất 1 nguyên liệu hợp lệ.");
    }
    var stepItems = steps.split(/\r?\n/).map(function (s) { return s.trim(); }).filter(function (s) { return s.length > 0; });
    if (stepItems.length === 0) {
        errors.push("Vui lòng nhập ít nhất 1 bước thực hiện.");
    }
    if (fileInput && fileInput.files.length > 0) {
        var fileName = fileInput.files[0].name.toLowerCase();
        if (!/\.(jpg|jpeg|png|webp|gif)$/.test(fileName)) {
            errors.push("File ảnh tải lên chỉ chấp nhận định dạng .jpg, .jpeg, .png hoặc .webp.");
        }
    }

    if (errors.length > 0) {
        if (errBox) {
            errBox.innerHTML = "⚠️ " + errors.join("<br/>⚠️ ");
            errBox.style.display = "block";
            window.scrollTo({ top: 0, behavior: 'smooth' });
        } else {
            alert(errors.join("\n"));
        }
        return false;
    }

    if (errBox) errBox.style.display = "none";
    return true;
}


// ==========================================================================
// 5. RATINGS, COMMENTS & DELETE ACTIONS (Đánh giá sao, Bình luận, Xóa món)
// ==========================================================================

/**
 * Tương tác chọn sao trong Modal Chi tiết món ăn & Trang độc lập (Fix TC10)
 */
$(document).on('click', '.rating-star', function () {
    var selectedValue = parseInt($(this).attr('data-value') || $(this).attr('data-val'), 10);
    if (!selectedValue) return;

    var $container = $(this).closest('.rating-stars');

    // Cập nhật đổi màu vàng cho tất cả các sao <= selectedValue
    $container.find('.rating-star').each(function () {
        var starVal = parseInt($(this).attr('data-value') || $(this).attr('data-val'), 10);
        if (starVal <= selectedValue) {
            $(this).addClass('active selected text-warning');
            $(this).css('color', '#f59e0b');
        } else {
            $(this).removeClass('active selected text-warning');
            $(this).css('color', '#d1d5db');
        }
    });

    // Nếu là click trong form trang độc lập (#pageStarBox)
    if ($container.attr('id') === 'pageStarBox' || $(this).closest('#pageStarBox').length > 0) {
        setPageRating(selectedValue);
        return;
    }

    // Nếu là click trong modal hoặc form khác
    $('#selectedRating').val(selectedValue);
    $('input[name="Rating"]').val(selectedValue);

    var labels = { 1: 'Rất tệ', 2: 'Chưa tốt', 3: 'Bình thường', 4: 'Tốt', 5: 'Tuyệt vời' };
    $('#starRatingLabel').text('(' + selectedValue + '/5 ' + (labels[selectedValue] || '') + ')');
});

// Hiệu ứng rê chuột (Hover) xem trước số sao
$(document).on('mouseenter', '.rating-stars .rating-star', function () {
    var hoverVal = parseInt($(this).attr('data-value') || $(this).attr('data-val'), 10);
    if (!hoverVal) return;
    var $container = $(this).closest('.rating-stars');
    $container.find('.rating-star').each(function () {
        var val = parseInt($(this).attr('data-value') || $(this).attr('data-val'), 10);
        if (val <= hoverVal) {
            $(this).css('color', '#f59e0b');
        } else {
            $(this).css('color', '#d1d5db');
        }
    });
});

// Khi rê chuột ra ngoài khu vực sao, phục hồi lại đúng số sao đã click chọn
$(document).on('mouseleave', '.rating-stars', function () {
    var $container = $(this);
    if ($container.attr('id') === 'pageStarBox' || $container.closest('#pageStarBox').length > 0) {
        setPageRating(pageSelectedStars);
        return;
    }
    var currentRating = parseInt($('#selectedRating').val() || $('input[name="Rating"]').val(), 10) || 0;
    $container.find('.rating-star').each(function () {
        var val = parseInt($(this).attr('data-value') || $(this).attr('data-val'), 10);
        if (val <= currentRating) {
            $(this).addClass('active selected text-warning').css('color', '#f59e0b');
        } else {
            $(this).removeClass('active selected text-warning').css('color', '#d1d5db');
        }
    });
});

/**
 * Chọn sao trên trang Chi Tiết Độc Lập (/Recipe/Details/{id})
 */
var pageSelectedStars = 5;

function setPageRating(val) {
    pageSelectedStars = val;
    $('#pageSelectedRating').val(val);
    $('input[name="Rating"]').val(val);
    var stars = document.querySelectorAll('#pageStarBox .rating-star');
    stars.forEach(function (st, idx) {
        if (idx < val) {
            st.classList.add('active', 'selected', 'text-warning');
            st.style.color = '#f59e0b';
        } else {
            st.classList.remove('active', 'selected', 'text-warning');
            st.style.color = '#d1d5db';
        }
    });

    var labels = { 1: ' (Cần cải thiện)', 2: ' (Tạm được)', 3: ' (Khá ổn)', 4: ' (Rất ngon)', 5: ' (Tuyệt vời)' };
    var labelText = val + ' Sao' + (labels[val] || '');
    var labelElem = document.getElementById('pageStarValueText');
    if (labelElem) labelElem.textContent = labelText;
}

function resetReviewForm() {
    $('#selectedRating').val(0);
    $('input[name="Rating"]').val(0);
    $('#reviewContent').val('');
    $('.rating-star').removeClass('active selected text-warning').css('color', '#d1d5db');
    $('#starRatingLabel').text('(Chưa chọn sao)');
}

/**
 * Gửi đánh giá sao & bình luận từ Modal
 */
function submitReview() {
    var recipeId = parseInt($('#reviewRecipeId').val(), 10);
    var stars = parseInt($('#selectedRating').val(), 10);
    var content = ($('#reviewContent').val() || '').trim();

    if (!recipeId || recipeId <= 0) { showCookingBabyToast('Không xác định được công thức.', 'error'); return; }
    if (isNaN(stars) || stars < 1 || stars > 5) { showCookingBabyToast('Vui lòng chọn từ 1 đến 5 sao.', 'error'); return; }
    if (content.length > 1000) { showCookingBabyToast('Bình luận đánh giá không được vượt quá 1000 ký tự.', 'error'); return; }

    var button = $('#btnSubmitReview');
    button.prop('disabled', true).text('Đang gửi...');

    $.ajax({
        url: '/Home/SubmitReview',
        type: 'POST',
        data: { recipeId: recipeId, stars: stars, content: content },
        success: function (res) {
            if (!res.success) {
                showCookingBabyToast(res.message || 'Không thể gửi đánh giá.', 'error');
                return;
            }
            resetReviewForm();
            loadReviews(recipeId);
            refreshRatingSummary(recipeId);
            showCookingBabyToast(res.message || 'Đánh giá thành công!', 'success');
        },
        error: function () {
            showCookingBabyToast('Có lỗi xảy ra khi gửi đánh giá. Vui lòng thử lại.', 'error');
        },
        complete: function () {
            button.prop('disabled', false).text('Gửi Đánh Giá');
        }
    });
}

/**
 * Tải danh sách đánh giá & bình luận cho Modal
 */
function loadReviews(recipeId) {
    var container = $('#reviewList');
    container.html('<p style="color:#718096;">Đang tải bình luận...</p>');

    $.ajax({
        url: '/Home/GetReviews',
        type: 'GET',
        data: { recipeId: recipeId },
        success: function (res) {
            container.empty();
            var reviews = Array.isArray(res) ? res : ((res && (res.reviews || res.Reviews)) || []);
            if (reviews.length === 0) {
                container.html('<p class="muted-note">Chưa có đánh giá nào. Hãy là người đầu tiên đánh giá món này!</p>');
                return;
            }

            reviews.forEach(function (review) {
                var userName = review.UserName || review.userName || 'Người dùng';
                var stars = parseInt(review.Stars || review.stars || 0, 10);
                var content = (review.Content || review.content || '').trim();
                var createdAt = review.CreatedAt || review.createdAt || '';
                var starsText = '★★★★★'.substring(0, stars) + '☆☆☆☆☆'.substring(0, 5 - stars);

                var reviewItem = $('<div>').addClass('review-item');
                var header = $('<div>').css({ display: 'flex', justifyContent: 'space-between', alignItems: 'center' });
                header.append($('<strong>').text(userName));
                header.append($('<small>').addClass('muted-note').text(createdAt));

                var starElement = $('<div>').addClass('review-stars').text(starsText);
                reviewItem.append(header, starElement);
                if (content) {
                    reviewItem.append($('<p>').addClass('muted-desc').text(content));
                }
                container.append(reviewItem);
            });
        },
        error: function () {
            container.html('<p class="text-danger">Không thể tải bình luận.</p>');
        }
    });
}

function refreshRatingSummary(recipeId) {
    $.ajax({
        url: '/Home/GetRecipeDetail',
        type: 'GET',
        data: { id: recipeId },
        success: function (res) {
            if (res && res.success) {
                updateRatingSummary(res.avgRating || 0, res.reviewCount || 0);
            }
        }
    });
}

function updateRatingSummary(avgRating, reviewCount) {
    avgRating = parseFloat(avgRating) || 0;
    reviewCount = parseInt(reviewCount, 10) || 0;
    $('#modalDetailRatingScore').text(avgRating.toFixed(1));
    $('#modalDetailReviewCount').text(reviewCount === 0 ? 'Chưa có đánh giá' : 'Điểm trung bình từ ' + reviewCount + ' đánh giá');
}

function renderIngredients(ingredients) {
    var container = $('#modalDetailIngredients');
    container.empty();
    if (!ingredients || ingredients.length === 0) {
        container.html('<p class="muted-note">Chưa có thông tin nguyên liệu.</p>');
        return;
    }
    ingredients.forEach(function (ingredient) {
        var label = $('<label>').addClass('detail-checklist-item');
        label.append($('<input>').attr('type', 'checkbox'));
        label.append($('<span>').text(ingredient));
        container.append(label);
    });
}

function renderSteps(steps) {
    var container = $('#modalDetailSteps');
    container.empty();
    if (!steps || steps.length === 0) {
        container.html('<p class="muted-note">Chưa có các bước thực hiện.</p>');
        return;
    }
    steps.forEach(function (step, index) {
        var row = $('<div>').addClass('detail-step-card');
        var number = $('<span>').addClass('step-number-pill').text(('0' + (index + 1)).slice(-2));
        var content = $('<p>').addClass('step-text').text(step);
        row.append(number, content);
        container.append(row);
    });
}

/**
 * Gửi đánh giá sao & bình luận từ Trang Chi Tiết Độc Lập (/Recipe/Details/{id})
 */
function submitPageReview(recipeId, rateUrl, commentUrl) {
    var commentInput = document.getElementById('pageCommentInput');
    var commentText = commentInput ? commentInput.value.trim() : '';

    if (commentText.length > 1000) {
        showCookingBabyToast('Bình luận đánh giá không được vượt quá 1000 ký tự.', 'error');
        return;
    }

    var submitBtn = document.getElementById('btnPageSubmitReview');
    var actualRateUrl = rateUrl || (submitBtn ? submitBtn.getAttribute('data-rate-url') : null) || '/Recipe/RateRecipe';

    if (submitBtn) submitBtn.disabled = true;
    $.ajax({
        url: actualRateUrl,
        type: 'POST',
        data: { id: recipeId, stars: pageSelectedStars, content: commentText },
        success: function (res) {
            if (res && res.success) {
                location.reload();
            } else {
                showCookingBabyToast((res && res.message) ? res.message : 'Không thể gửi đánh giá.', 'error');
            }
        },
        error: function () {
            showCookingBabyToast('Lỗi kết nối khi gửi đánh giá. Vui lòng thử lại.', 'error');
        },
        complete: function () {
            if (submitBtn) submitBtn.disabled = false;
        }
    });
}

// Bắt sự kiện click nút gửi nhận xét trên trang độc lập qua data-attributes
$(document).on('click', '#btnPageSubmitReview', function () {
    var recipeId = $(this).data('recipe-id') || $(this).attr('data-recipe-id');
    var rateUrl = $(this).data('rate-url') || $(this).attr('data-rate-url');
    var commentUrl = $(this).data('comment-url') || $(this).attr('data-comment-url');
    submitPageReview(recipeId, rateUrl, commentUrl);
});
