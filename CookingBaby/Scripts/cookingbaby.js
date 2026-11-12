// =====================================================================
// 1. ĐIỀU KHIỂN ĐÓNG/MỞ MODAL ĐĂNG CÔNG THỨC MỚI
// =====================================================================
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

window.addEventListener('click', function (event) {
    var modal = document.getElementById('createRecipeModal');
    if (event.target === modal) {
        $(modal).removeClass('active');
    }
});

// =====================================================================
// 2. KIỂM TRA DỮ LIỆU ĐẦU VÀO CHO MODAL ĐĂNG CÔNG THỨC MỚI
// =====================================================================
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
        if (!/\.(jpg|jpeg|png|webp)$/.test(fileName)) {
            errors.push("File ảnh đại diện chỉ chấp nhận định dạng .jpg, .jpeg, .png hoặc .webp.");
        }
    }

    if (errors.length > 0) {
        alert("⚠️ Vui lòng kiểm tra lại thông tin:\n\n- " + errors.join("\n- "));
        return false;
    }

    return true;
}


// =====================================================================
// 3. CHI TIẾT CÔNG THỨC, ĐÁNH GIÁ, BÌNH LUẬN VÀ LƯU/BỎ LƯU
// =====================================================================
$(document).on('click', '.rating-star', function () {
    var selectedValue = parseInt($(this).attr('data-value'), 10);
    $('#selectedRating').val(selectedValue);

    $('.rating-star').each(function () {
        var starValue = parseInt($(this).attr('data-value'), 10);
        $(this).toggleClass('active', starValue <= selectedValue);
    });

    var labels = { 1: 'Rất tệ', 2: 'Chưa tốt', 3: 'Bình thường', 4: 'Tốt', 5: 'Tuyệt vời' };
    $('#starRatingLabel').text('(' + selectedValue + '/5 ' + labels[selectedValue] + ')');
});

function resetReviewForm() {
    $('#selectedRating').val(0);
    $('#reviewContent').val('');
    $('.rating-star').removeClass('active');
    $('#starRatingLabel').text('(Chưa chọn sao)');
}

function submitReview() {
    var recipeId = parseInt($('#reviewRecipeId').val(), 10);
    var stars = parseInt($('#selectedRating').val(), 10);
    var content = ($('#reviewContent').val() || '').trim();

    if (!recipeId || recipeId <= 0) { alert('Không xác định được công thức.'); return; }
    if (isNaN(stars) || stars < 1 || stars > 5) { alert('Vui lòng chọn từ 1 đến 5 sao.'); return; }
    if (content.length === 0) { alert('Vui lòng nhập nội dung bình luận.'); return; }
    if (content.length > 1000) { alert('Bình luận không được vượt quá 1000 ký tự.'); return; }

    var button = $('#btnSubmitReview');
    button.prop('disabled', true).text('Đang gửi...');

    $.ajax({
        url: '/Home/SubmitReview',
        type: 'POST',
        data: { recipeId: recipeId, stars: stars, content: content },
        success: function (res) {
            if (!res.success) { alert(res.message || 'Không thể gửi đánh giá.'); return; }
            resetReviewForm();
            loadReviews(recipeId);
            refreshRatingSummary(recipeId);
            alert(res.message || 'Đánh giá thành công!');
        },
        error: function () { alert('Có lỗi xảy ra khi gửi đánh giá.'); },
        complete: function () { button.prop('disabled', false).text('Gửi Đánh Giá'); }
    });
}

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
                container.html('<p style="font-size:13px;color:#718096;">Chưa có đánh giá nào. Hãy là người đầu tiên đánh giá món này!</p>');
                return;
            }

            reviews.forEach(function (review) {
                var userName = review.UserName || review.userName || 'Người dùng';
                var stars = parseInt(review.Stars || review.stars || 0, 10);
                var content = review.Content || review.content || '';
                var createdAt = review.CreatedAt || review.createdAt || '';
                var starsText = '★★★★★'.substring(0, stars) + '☆☆☆☆☆'.substring(0, 5 - stars);

                var reviewItem = $('<div>').addClass('review-item');
                var header = $('<div>').css({ display: 'flex', justifyContent: 'space-between', alignItems: 'center' });
                header.append($('<strong>').text(userName));
                header.append($('<small>').css('color', '#A0AEC0').text(createdAt));

                var starElement = $('<div>').addClass('review-stars').text(starsText);
                var contentElement = $('<p>').css({ margin: '5px 0 12px 0', fontSize: '13px', color: '#4A5568', lineHeight: '1.5' }).text(content);

                reviewItem.append(header, starElement, contentElement);
                container.append(reviewItem);
            });
        },
        error: function () { container.html('<p style="color:#E53E3E;">Không thể tải bình luận.</p>'); }
    });
}

function refreshRatingSummary(recipeId) {
    $.ajax({
        url: '/Home/GetRecipeDetail',
        type: 'GET',
        data: { id: recipeId },
        success: function (res) {
            if (res && res.success) { updateRatingSummary(res.avgRating || 0, res.reviewCount || 0); }
        }
    });
}

function updateRatingSummary(avgRating, reviewCount) {
    avgRating = parseFloat(avgRating) || 0;
    reviewCount = parseInt(reviewCount, 10) || 0;
    $('#modalDetailRatingScore').text(avgRating.toFixed(1));
    $('#modalDetailReviewCount').text(reviewCount === 0 ? 'Chưa có đánh giá' : 'Điểm trung bình từ ' + reviewCount + ' đánh giá');
}

function toggleSaved(recipeId, btn, isSavedPage) {
    if (!recipeId) return;

    $.ajax({
        url: '/Home/ToggleSaved',
        type: 'POST',
        data: { recipeId: recipeId },
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
                $(btn).addClass('saved').attr('title', 'Bỏ lưu món này');
                $(btn).text('♥');
            } else {
                $(btn).removeClass('saved').attr('title', 'Lưu món này');
                $(btn).text('♡');
            }

            if (isSavedPage && !res.isSaved) {
                $('#recipe-card-' + recipeId).fadeOut(300, function () {
                    $(this).remove();
                    if ($('.saved-card').length === 0 || $('#savedRecipeGrid .saved-card').length === 0) {
                        location.reload();
                    }
                });
            }

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

function renderIngredients(ingredients) {
    var container = $('#modalDetailIngredients');
    container.empty();
    if (!ingredients || ingredients.length === 0) {
        container.html('<p style="color:#718096;">Chưa có thông tin nguyên liệu.</p>');
        return;
    }
    ingredients.forEach(function (ingredient) {
        var label = $('<label>').css({ background: '#F8FAFC', border: '1px solid #E2E8F0', padding: '8px 12px', borderRadius: '10px', fontSize: '13px', display: 'flex', alignItems: 'center', gap: '9px', cursor: 'pointer' });
        label.append($('<input>').attr('type', 'checkbox').css('cursor', 'pointer'));
        label.append($('<span>').text(ingredient));
        container.append(label);
    });
}

function renderSteps(steps) {
    var container = $('#modalDetailSteps');
    container.empty();
    if (!steps || steps.length === 0) {
        container.html('<p style="color:#718096;">Chưa có các bước thực hiện.</p>');
        return;
    }
    steps.forEach(function (step, index) {
        var row = $('<div>').css({ display: 'flex', gap: '12px', alignItems: 'flex-start' });
        var number = $('<span>').text(('0' + (index + 1)).slice(-2)).css({ background: '#FF7A00', color: '#FFF', fontSize: '11px', fontWeight: '800', width: '28px', height: '28px', borderRadius: '50%', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: '0' });
        var content = $('<p>').text(step).css({ margin: '4px 0 0 0', fontSize: '13px', color: '#4A5568', lineHeight: '1.5' });
        row.append(number, content);
        container.append(row);
    });
}
var currentModalRecipeId = 0; // Biến toàn cục lưu trữ ID món đang mở trong modal

function openDetailModal(recipeId) {
    currentModalRecipeId = recipeId; // Gán lại ID hiện tại

    $.ajax({
        url: '/Recipe/GetRecipeDetail',
        type: 'GET',
        data: { id: recipeId },
        success: function (res) {
            if (!res || !res.success) { alert('Không tìm thấy thông tin công thức.'); return; }

            $('#reviewRecipeId').val(recipeId);
            $('#modalDetailTitle').text(res.title || '');
            $('#modalDetailCat').text(res.categoryName || 'Món ngon');
            $('#modalDetailTime').text('⏱ ' + (res.cookTime || 0) + ' phút');
            $('#modalDetailDesc').text(res.description || '');
            $('#modalDetailAuthor').text(res.authorName || 'Không xác định');

            // 1. Xử lý trạng thái nút Lưu chuẩn xác theo database (Không bị hiện cứng mặc định)
            var saveBtn = $('#modalToggleSaveBtn');
            if (res.isSaved) {
                saveBtn.addClass('saved').css('background', '#E53E3E').text('♥ Đã lưu trong sổ tay');
            } else {
                saveBtn.removeClass('saved').css('background', '#FF5A1F').text('♡ Lưu vào sổ tay');
            }

            // 2. Xử lý quyền tác giả: So sánh đúng ID để hiện nút Sửa/Xóa
            if (res.authorId === res.currentUserId) {
                $('#modalAuthorActions').css('display', 'flex'); // Hiện cụm nút Sửa/Xóa
                $('#modalAuthorPermission').hide();              // Ẩn dòng chữ thông báo hạn chế
                $('#modalEditBtn').attr('href', '/Recipe/EditRecipe/' + res.recipeId);
                $('#modalDeleteId').val(res.recipeId);
            } else {
                $('#modalAuthorActions').hide();                 // Ẩn cụm nút Sửa/Xóa đi
                $('#modalAuthorPermission').show();             // Hiện lại dòng thông báo
            }

            var imageName = res.imageUrl || 'supbido.jpg';
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
        error: function () { alert('Không thể tải chi tiết công thức.'); }
    });
}

// Hàm hỗ trợ bấm nút Lưu trực tiếp ngay trong Modal chi tiết
function toggleSavedFromModal() {
    if (!currentModalRecipeId) return;
    var btn = $('#modalToggleSaveBtn');

    // Gọi chung hàm toggleSaved đã có sẵn trong hệ thống của bạn
    toggleSaved(currentModalRecipeId, btn, false);

    // Cập nhật giao diện nút trong modal sau khi bấm
    setTimeout(function () {
        if (btn.hasClass('saved') || btn.text().indexOf('Đã lưu') >= 0) {
            btn.css('background', '#E53E3E').text('♥ Đã lưu trong sổ tay');
        } else {
            btn.css('background', '#FF5A1F').text('♡ Lưu vào sổ tay');
        }
    }, 200);
}

function closeDetailModal() {
    $('#recipeDetailModal').css('display', 'none').removeClass('active');
}

// Thêm AntiForgeryToken vào mọi AJAX POST trên cùng website
$.ajaxPrefilter(function (options) {
    if ((options.type || '').toUpperCase() !== 'POST') return;
    if (/^https?:\/\//i.test(options.url) && new URL(options.url, window.location.href).origin !== window.location.origin) return;
    var token = $('#ajaxAntiForgery input[name="__RequestVerificationToken"]').val() || $('input[name="__RequestVerificationToken"]').val();
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

// Preview ảnh trong Modal Tạo công thức mới
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
            $('#createPreviewBox').css('display', 'flex');
        };
        reader.readAsDataURL(file);
    } else {
        $('#createPreviewBox').hide();
    }
}
