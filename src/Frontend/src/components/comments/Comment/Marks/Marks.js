import React from "react";
import PropTypes from "prop-types";
import { comment, group } from "../../commonPropTypes";
import Icon from "@skbkontur/react-icons";
import TooltipMenu from "@skbkontur/react-ui/components/TooltipMenu/TooltipMenu";
import MenuItem from "@skbkontur/react-ui/components/MenuItem/MenuItem";
import MenuSeparator from "@skbkontur/react-ui/components/MenuSeparator/MenuSeparator";
import MenuHeader from "@skbkontur/react-ui/components/MenuHeader/MenuHeader";

import styles from "./Marks.less";

export default function Marks({courseId, comment, canViewStudentsGroup, authorGroups}) {
	console.log(authorGroups);
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

const GroupMark = ({url, groups}) => (
		<>
			<div className={styles.visibleOnDesktopAndTablet}>
				<div className={styles.groupList}>
					{groups.map(group =>
						<div key={group.id} className={`${styles.mark} ${styles.group} ${group.isArchived && styles.archiveGroup}`}>
							<Icon name="People" size={15} />
							<a href={group.apiUrl && `${url}${group.apiUrl}`}
							   className={`${styles.text} ${styles.groupName}`}>
								{group.name}
							</a>
						</div>)}
				</div>
			</div>
			<div className={styles.visibleOnPhone}>
				<TooltipMenu
					menuWidth="150px"
					positions={["bottom right"]}
					caption={<div className={styles.tooltipGroups}>
						<Icon name="People" color="#fff" size={15} />
					</div>}>
					<MenuHeader><span className={styles.tooltipGroupsHeader}>Группы</span></MenuHeader>
					<MenuSeparator />
					{groups.map(group => !group.isArchived &&
						<MenuItem
							key={group.id}
							href={group.apiUrl && `${url}${group.apiUrl}`}>
							<span className={styles.tooltipGroupName}>{group.name}</span>
						</MenuItem>)}
					<MenuSeparator />
					{groups.map(group => group.isArchived &&
						<MenuItem
							key={group.id}
							href={group.apiUrl && `${url}${group.apiUrl}`}>
							<span className={styles.tooltipGroupName}>{group.name}</span>
						</MenuItem>)}
				</TooltipMenu>
			</div>
		</>
);

Marks.propTypes = {
	authorGroups: PropTypes.arrayOf(group),
	courseId: PropTypes.string,
	comment: comment.isRequired,
	canViewStudentsGroup: PropTypes.func,
};
