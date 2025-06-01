// Post interaction functions
function toggleLike(postId, element) {
    if (!postId) return;

    // Find if we're dealing with the stats area or the button
    const isStatsElement = $(element).hasClass('like-stats');

    // Get the liked state from the element that was clicked
    const isLiked = $(element).data('liked') === true;
    const url = isLiked ? '/Posts/Unlike/' + postId : '/Posts/Like/' + postId;

    $.ajax({
        url: url,
        type: 'POST',
        success: function (response) {
            if (response.success) {
                const newLikedState = !isLiked;

                // Update element data attribute
                $(element).data('liked', newLikedState);

                // Handle different like count elements depending on the page
                let likeCount;

                // For the details page
                if (isStatsElement) {
                    likeCount = parseInt($('#likesCount').text());
                    likeCount = newLikedState ? likeCount + 1 : Math.max(0, likeCount - 1);
                    $('#likesCount').text(likeCount);

                    // Update icon styling
                    if (newLikedState) {
                        $(element).find('i').removeClass('bi-hand-thumbs-up').addClass('bi-hand-thumbs-up-fill text-primary');
                    } else {
                        $(element).find('i').removeClass('bi-hand-thumbs-up-fill text-primary').addClass('bi-hand-thumbs-up');
                    }
                }
                // For the feed/profile pages
                else {
                    const likeCountElement = $(element).find('.like-count');
                    likeCount = parseInt(likeCountElement.text());
                    likeCount = newLikedState ? likeCount + 1 : Math.max(0, likeCount - 1);
                    likeCountElement.text(likeCount);

                    // Update button styling
                    if (newLikedState) {
                        $(element).removeClass('text-muted').addClass('text-primary');
                        $(element).find('i').removeClass('bi-hand-thumbs-up').addClass('bi-hand-thumbs-up-fill');
                    } else {
                        $(element).removeClass('text-primary').addClass('text-muted');
                        $(element).find('i').removeClass('bi-hand-thumbs-up-fill').addClass('bi-hand-thumbs-up');
                    }
                }
            }
        },
        error: function () {
            alert('Unable to process your request. Please try again.');
        }
    });
}

function toggleCommentForm(postId) {
    const commentSection = $('#comment-section-' + postId);
    commentSection.slideToggle(200);

    // Focus on the input if opening
    if (commentSection.is(':hidden')) {
        setTimeout(() => {
            $('#comment-input-' + postId).focus();
        }, 250);
    }
}

function addComment(postId) {
    const commentInput = $('#comment-input-' + postId);
    const content = commentInput.val().trim();

    if (!content) return;

    $.ajax({
        url: '/Posts/AddComment',
        type: 'POST',
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        data: {
            postId: postId,
            content: content
        },
        success: function (response) {
            if (response.success) {
                // Create new comment element
                const commentHtml = `
                    <div class="d-flex mb-2">
                        <img src="${response.userProfilePicture}" class="rounded-circle me-2"
                            alt="Profile" style="width: 32px; height: 32px; object-fit: cover;">
                        <div class="bg-light p-2 rounded flex-grow-1">
                            <div class="d-flex justify-content-between">
                                <a href="/Account/ViewProfile/${response.userId}" class="text-decoration-none fw-bold small">
                                    ${response.userName}
                                </a>
                                <small class="text-muted">Just now</small>
                            </div>
                            <p class="mb-0 small">${response.content}</p>
                        </div>
                    </div>
                `;

                // Add to comments section
                const existingComments = $('#comment-section-' + postId + ' .existing-comments');
                if (existingComments.length) {
                    existingComments.append(commentHtml);
                } else {
                    $('<div class="existing-comments mb-3"></div>')
                        .append(commentHtml)
                        .insertBefore('#comment-section-' + postId + ' .d-flex:last');
                }

                // Update comment count in post item
                const commentButton = $(`button[onclick="toggleCommentForm(${postId})"]`);
                if (commentButton.length > 0) {
                    commentButton.find('span').text(response.commentCount);
                }

                // Update comment count on details page if present
                $('#commentsCount').text(response.commentCount);

                // Clear input
                commentInput.val('');
            }
        },
        error: function () {
            alert('Unable to add your comment. Please try again.');
        }
    });
}

function addDetailComment(postId) {
    const content = $('#commentInput').val().trim();
    if (!content) return;

    $.ajax({
        url: '/Posts/AddComment',
        type: 'POST',
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        data: {
            postId: postId,
            content: content
        },
        success: function (response) {
            if (response.success) {
                // Create new comment HTML for details page
                const commentHtml = `
                    <div class="d-flex mb-3">
                        <img src="${response.userProfilePicture}" class="rounded-circle me-2"
                            alt="Profile" style="width: 32px; height: 32px; object-fit: cover;">
                        <div class="flex-grow-1">
                            <div class="bg-light p-2 rounded">
                                <div class="d-flex justify-content-between align-items-center mb-1">
                                    <a href="/Account/ViewProfile/${response.userId}" class="text-decoration-none fw-bold">
                                        ${response.userName}
                                    </a>
                                    <small class="text-muted">Just now</small>
                                </div>
                                <p class="mb-0">${response.content}</p>
                            </div>
                        </div>
                    </div>
                `;

                // Add to comments container
                $('#comments-container').append(commentHtml);

                // Update comment count
                $('#commentsCount').text(response.commentCount);

                // Clear input
                $('#commentInput').val('');
            }
        },
        error: function () {
            alert('Unable to add your comment. Please try again.');
        }
    });
}

// Initialize event listeners when document is ready
$(document).ready(function () {
    console.log("Initializing post interactions...");

    // Remove ALL previous event handlers first
    $(document).off('keypress', 'input[id^="comment-input-"]');
    $(document).off('keypress', '#commentInput');
    $('#commentInput').off('keypress');

    // Clear any inline event handlers that might have been added
    $('#commentInput').attr('onkeypress', '');

    // Single handler for regular comment inputs (feed/profile pages)
    $(document).on('keypress.commentHandler', 'input[id^="comment-input-"]:not(#commentInput)', function (e) {
        console.log("Regular comment keypress detected");
        if (e.which === 13) {
            console.log("Enter pressed on regular comment");
            e.preventDefault();
            e.stopImmediatePropagation();

            const postId = this.id.replace('comment-input-', '');
            addComment(postId);
            return false;
        }
    });

    // Separate handler specifically for the details page comment input
    $(document).on('keypress.detailCommentHandler', '#commentInput', function (e) {
        console.log("Detail comment keypress detected");
        if (e.which === 13) {
            console.log("Enter pressed on detail comment");
            e.preventDefault();
            e.stopImmediatePropagation();

            const postId = $(this).data('post-id');
            console.log("Adding detail comment for post ID:", postId);
            addDetailComment(postId);
            return false;
        }
    });

    // Focus comment input when comment button is clicked in details view
    window.focusCommentInput = function () {
        document.getElementById('commentInput').focus();
    };
});