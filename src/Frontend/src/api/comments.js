import api from "./index";

export function getComments(courseId, slideId, isForInstructors) {
	return api.get(`comments?course_id=${courseId}&slide_id=${slideId}&for_instructors=`
		+ encodeURIComponent(isForInstructors));
}

export function getComment(commentId) {
	return api.get(`comments/${commentId}`);
}

export function addComment(courseId, slideId, text, parentCommentId, forInstructors) {
	return api.post(`comments?course_id=${courseId}`,
		api.createRequestParams({
			slideId,
			text,
			parentCommentId,
			forInstructors,
		})
	);
}

export function deleteComment(commentId) {
	return api.delete(`comments/${commentId}`);
}

export function updateComment(commentId, commentSettings = {}) {
	return api.patch(`comments/${commentId}`,
		api.createRequestParams(commentSettings)
	);
}

export function likeComment(commentId) {
	return api.post(`comments/${commentId}/like`);
}

export function dislikeComment(commentId) {
	return api.delete(`comments/${commentId}/like`);
}

export function getCommentPolicy(courseId) {
	return api.get(`comment-policies?course_id=${courseId}`);
}

export function updateCommentPolicy(courseId, commentsPolicySettings) {
	return api.patch(`comment-policies?course_id=${courseId}`,
		api.createRequestParams(commentsPolicySettings)
	);
}

