import React from "react";
import { storiesOf } from "@storybook/react";
import { action } from "@storybook/addon-actions";
import { withViewport } from "@storybook/addon-viewport";
import CommentsPolicy from "./CommentsPolicy";

storiesOf("Comments/CommentsView", module)
.addDecorator(withViewport())
.add("comment policy", () => (
	<CommentsPolicy
		canComment={true}
		handleSubmit={action("Настройки сохранены")}
	/>
), {viewport: "desktop"});