import React, {Component} from 'react';
import {Switch, Route, Redirect} from 'react-router-dom';

import AnyPage from "./pages/AnyPage";
import GroupListPage from "./pages/course/groups/GroupListPage";
import GroupPage from "./pages/course/groups/GroupPage";
import Course from './pages/course/CoursePage';

import {getQueryStringParameter} from "./utils";


function Router() {
	return (
		<Switch>
			<Route path="/Admin/Groups" component={redirectLegacyPage("/:courseId/groups")}/>

			<Route path="/course/:courseId/:slideId" component={Course}/>

			<Route path="/:courseId/groups/" component={GroupListPage} exact/>
			<Route path="/:courseId/groups/:groupId/" component={GroupPage} exact/>
			<Route path="/:courseId/groups/:groupId/:groupPage" component={GroupPage} exact/>

			<Route component={AnyPage}/>
		</Switch>
	)
}

function redirectLegacyPage(to) {
	return class extends Component {
		constructor(props) {
			super(props);
			let courseId = getQueryStringParameter("courseId");
			if (courseId)
				to = to.replace(":courseId", courseId);
		}

		render() {
			return <Redirect to={to}/>;
		}
	};
}

export default Router;
