import React from "react";
import PropTypes from "prop-types";
import { comment, group } from "../../commonPropTypes";
import Icon from "@skbkontur/react-icons";
import TooltipMenu from "@skbkontur/react-ui/components/TooltipMenu/TooltipMenu";
import MenuItem from "@skbkontur/react-ui/components/MenuItem/MenuItem";
import MenuSeparator from "@skbkontur/react-ui/components/MenuSeparator/MenuSeparator";
import MenuHeader from "@skbkontur/react-ui/components/MenuHeader/MenuHeader";
import Hint from "@skbkontur/react-ui/components/Hint/Hint";

import styles from "./Marks.less";

export default function Marks({courseId, comment, canViewStudentsGroup, authorGroups}) {
	const windowUrl = `${window.location.origin}/${courseId}`;
	return (
		<>
			{!comment.isApproved && <HiddenMark />}
			{comment.isCorrectAnswer && <CorrectAnswerMark />}
			{comment.isPinnedToTop && <PinnedToTopMark />}
			{authorGroups && <GroupMark url={windowUrl} groups={authorGroups} />}
		</>
	)
};

const HiddenMark = () => (
	<div className={`${styles.mark} ${styles.approvedComment}`}>
		<Icon name="EyeClosed" size={15} />
		<span className={`${styles.text} ${styles.visibleOnDesktopAndTablet}`}>
			Скрыт
		</span>
	</div>
);

const CorrectAnswerMark = () => (
	<div className={`${styles.mark} ${styles.correctAnswer}`}>
		<Icon name="Ok" size={15} />
		<span className={`${styles.text} ${styles.visibleOnDesktopAndTablet}`}>
			Правильный&nbsp;ответ
		</span>
	</div>
);

const PinnedToTopMark = () => (
	<div className={`${styles.mark} ${styles.pinnedToTop}`}>
		<Icon name="Pin" size={15} />
		<span className={`${styles.text} ${styles.visibleOnDesktopAndTablet}`}>
			Закреплен
		</span>
	</div>
);

export function GroupMark({url, groups}) {
	const activeGroups = groups.filter(group => !group.isArchived);
	const archivedGroups = groups.filter(group => group.isArchived);

	return (
		<>
			<div className={styles.visibleOnDesktopAndTablet}>
				<div className={styles.groupList}>
					{activeGroups && activeGroups.map(group =>
						renderGroupsList(url, group, "activeGroup", "activeGroupName"))}
					{archivedGroups && archivedGroups.map(group =>
						renderGroupsList(url, group, "archivedGroup", "archivedGroupName"))}
				</div>
			</div>
			<div className={styles.visibleOnPhone}>
				<TooltipMenu
					menuWidth="150px"
					positions={["bottom right"]}
					caption={<div className={styles.tooltipGroups}>
						<Icon name="People" color="#fff" size={15} />
					</div>}>
					<MenuHeader>
						<span className={`${styles.tooltipGroupsHeader} ${styles.activeGroupName}`}>
							Группы
						</span>
					</MenuHeader>
					<MenuSeparator />
					{activeGroups && activeGroups.map(group =>
						renderTooltipGroupsList(url, group))}
					{archivedGroups && archivedGroups.map(group =>
						renderTooltipGroupsList(url, group, "archivedGroupName"))}
				</TooltipMenu>
			</div>
		</>)
};

export function renderGroupsList(url, group, groupStyle, groupName) {
	return (
		<Hint key={group.id} pos="right middle" text={group.name}>
			<div className={`${styles.mark} ${styles.group} ${styles[groupStyle]}`}>
				<Icon name="People" size={15} />
				<a href={group.apiUrl && `${url}${group.apiUrl}`}
				   className={`${styles.text} ${styles[groupName]}`}>
					{group.name}
				</a>
			</div>
		</Hint>
	);
};

export function renderTooltipGroupsList(url, group, groupName) {
	return (
		<MenuItem
			key={group.id}
			href={group.apiUrl && `${url}${group.apiUrl}`}>
			<span className={`${styles.tooltipGroupName} ${styles[groupName]}`}>{group.name}</span>
		</MenuItem>
	)
};

Marks.propTypes = {
	authorGroups: PropTypes.arrayOf(group),
	courseId: PropTypes.string,
	comment: comment.isRequired,
	canViewStudentsGroup: PropTypes.func,
};
