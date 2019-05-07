import React, { Component } from "react";
import PropTypes from "prop-types";
import { userRoles, user } from "../commonPropTypes";
import api from "../../../api";
import { TABS, ROLES } from "../../../consts/general";
import Icon from "@skbkontur/react-icons";
import Tabs from "@skbkontur/react-ui/components/Tabs/Tabs";
import Button from "@skbkontur/react-ui/components/Button/Button";
import CommentsList from "../CommentsList/CommentsList";
import CommentsPolicy from "../CommentsPolicy/CommentsPolicy";

import styles from "./CommentsView.less";

class CommentsView extends Component {
	constructor(props) {
		super(props);

		this.state = {
			instructorsComments: [],
			commentsPolicy: {},
			activeTab: TABS.allComments,
			openWindowSettings: false,
			instructorsCommentCount: 0,
		};

		this.headerRef = React.createRef();
	}

	static defaultProps = {
		commentsApi: api.comments,
	};

	componentDidMount() {
		const {courseId, slideId, userRoles} = this.props;

		this.loadCommentPolicy(courseId);

		if (this.isInstructor(userRoles))
			this.loadComments(courseId, slideId);
	};

	loadCommentPolicy = (courseId) => {
		this.props.commentsApi.getCommentPolicy(courseId)
			.then(commentsPolicy => {
				this.setState ({
					commentsPolicy: commentsPolicy,
				})
			})
			.catch(console.error);
	};

	loadComments = (courseId, slideId) => {
		this.props.commentsApi.getComments(courseId, slideId, true)
		.then(json => {
			let comments = json.topLevelComments;
			this.setState({
				instructorsComments: comments,
				instructorsCommentCount: comments.length,
			});
		})
		.catch(console.error);
	};

	render() {
		const {openWindowSettings, commentsPolicy} = this.state;

		return (
			<div className={styles.wrapper}>
				{this.renderHeader()}
				{openWindowSettings &&
				<CommentsPolicy
					commentsPolicy={commentsPolicy}
					handleClose={this.handleCloseWindowSettings}
					handleSubmit={this.handleSaveSettings} />}
				{this.renderComments()}
			</div>
		)
	};

	renderHeader() {
		const {userRoles} = this.props;
		const {activeTab, instructorsCommentCount} = this.state;

		return (
			<header className={styles.header} ref={this.headerRef}>
				<div className={styles.headerRow}>
					<h1 className={styles.headerName}>Комментарии</h1>
					{(userRoles.isSystemAdministrator || userRoles.courseRole === 'courseAdmin') &&
					<Button
						size="small"
						icon={<Icon.Settings />}
						onClick={this.handleOpenWindowSettings}>
						Настроить
					</Button>}
				</div>
				{this.isInstructor(userRoles) &&
				<div className={styles.tabs}>
					<Tabs value={activeTab} onChange={(e, id)=> this.handleTabChange(id, true)}>
						<Tabs.Tab id={TABS.allComments}>К слайду</Tabs.Tab>
						<Tabs.Tab id={TABS.instructorsComments}>
							Для преподавателей
							{instructorsCommentCount > 0 &&
							<span className={styles.commentsCount}>{instructorsCommentCount}</span>}
						</Tabs.Tab>
						{activeTab === TABS.instructorsComments &&
						<span className={`${styles.textForInstructors} ${styles["visible-at-least-tablet"]}`}>
							Студенты не видят эти комментарии
						</span>}
					</Tabs>
				</div>}
			</header>
		)
	};

	renderComments() {
		const {user, userRoles, courseId, slideId, slideType, commentsApi} = this.props;
		const {activeTab, commentsPolicy} = this.state;

		return (
			<div key={this.state.activeTab}>
				<CommentsList
					slideType={slideType}
					handleInstructorsCommentCount={this.handleInstructorsCommentCount}
					handleTabChange={this.handleTabChange}
					headerRef={this.headerRef}
					forInstructors={activeTab === TABS.instructorsComments}
					commentsApi={commentsApi}
					commentPolicy={commentsPolicy}
					user={user}
					userRoles={userRoles}
					slideId={slideId}
					courseId={courseId}>
				</CommentsList>
			</div>
		)
	}

	isCourseAdmin(userRoles) {
		return userRoles.isSystemAdministrator ||
			userRoles.courseRole === ROLES.courseAdmin;
	}

	isInstructor(userRoles) {
		return this.isCourseAdmin(userRoles) ||
			userRoles.courseRole === ROLES.instructor;
	}

	handleTabChange = (id, isUserAction) => {
		if (id !== this.state.activeTab) {
			this.setState({
				activeTab: id,
			});
		}

		if (isUserAction) {
			window.history.pushState("", document.title, window.location.pathname + window.location.search);
		}
	};

	handleInstructorsCommentCount = (action) => {
		if (action === "add") {
			this.setState({
				instructorsCommentCount: this.state.instructorsCommentCount + 1,
			})
		} else {
			this.setState({
				instructorsCommentCount: this.state.instructorsCommentCount - 1,
			})
		}
	};

	handleOpenWindowSettings = () => {
		this.setState({
			openWindowSettings: true,
		});
	};

	handleCloseWindowSettings = () => {
		this.setState({
			openWindowSettings: false,
		})
	};

	handleSaveSettings = (commentsEnabled, moderationValue) => {
		const commentsPolicySettings = {
			areCommentsEnabled: commentsEnabled,
			moderationPolicy: moderationValue,
			onlyInstructorsCanReply: false,
		};

		this.props.commentsApi.updateCommentPolicy(this.props.courseId, commentsPolicySettings);
		this.setState({
			commentsPolicy: commentsPolicySettings,
		});
		this.handleCloseWindowSettings();
	};
}

CommentsView.propTypes = {
	user: user.isRequired,
	userRoles: userRoles.isRequired,
	courseId: PropTypes.string.isRequired,
	slideId: PropTypes.string.isRequired,
	slideType: PropTypes.string.isRequired,
};

export default CommentsView;