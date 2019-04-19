import React, { Component } from 'react';
import Modal from "@skbkontur/react-ui/components/Modal/Modal";
import Toggle from "@skbkontur/react-ui/components/Toggle/Toggle";
import RadioGroup from "@skbkontur/react-ui/components/RadioGroup/RadioGroup";
import Radio from "@skbkontur/react-ui/components/Radio/Radio";
import Gapped from "@skbkontur/react-ui/components/Gapped/Gapped";
import Button from "@skbkontur/react-ui/components/Button/Button";

import styles from "./CommentsPolicy.less";

class CommentsPolicy extends Component {
	constructor(props) {
		super(props);

		this.state = {
			moderationValue: null,
			commentsEnabled: props.commentsPolicy.areCommentsEnabled,
		}
	}
	render() {
		const {handleClose} = this.props;

		return (
			<Modal onClose={handleClose} width={490}>
				<Modal.Header>Настройки комментариев курса</Modal.Header>
				<Modal.Body>
					<div className={styles.toggleEnabled}>
						<Toggle
							defaultChecked
							checked={this.state.commentsEnabled}
							onChange={() => this.handleToggle(this.state.commentsEnabled)}
						/>{' '}
						Студенты {this.state.commentsEnabled ? '' : 'не'} могут оставлять комментарии
					</div>
					<div className={styles.moderation}>
						<span className={styles.moderationHeader}>Модерация комментариев</span>
						<RadioGroup
							onChange={this.handleModeration}
							name="moderation"
							defaultValue={this.state.commentsEnabled && "postmoderation"}
							disabled={!this.state.commentsEnabled}>
							<Gapped vertical gap={10}>
								<Radio value="premoderation">Премодерация
									<span className={styles.moderationDescription}>
										Комментарии студентов по умоланию скрыты
									</span>
								</Radio>
								<Radio  value="postmoderation">Постмодерация
									<span className={styles.moderationDescription}>
										Комментарии студентов по умоланию открыты
									</span>
								</Radio>
							</Gapped>
						</RadioGroup>
					</div>
				</Modal.Body>
				<Modal.Footer panel={true}>
					<Gapped gap={10}>
						<Button use="primary" onClick={this.handleSubmit}>Сохранить</Button>
						<Button onClick={handleClose}>Отменить</Button>
					</Gapped>
				</Modal.Footer>
			</Modal>
		)
	}

	handleToggle = (isEnabled) => {
		this.setState({
			commentsEnabled: !isEnabled,
		});
	};

	handleModeration = (_, value) => {
		this.setState({
			moderationValue: value,
		})
	};

	handleSubmit = () => {
		const {commentsEnabled, moderationValue} = this.state;

		this.props.handleSubmit(commentsEnabled, moderationValue);
	}
}

export default CommentsPolicy;