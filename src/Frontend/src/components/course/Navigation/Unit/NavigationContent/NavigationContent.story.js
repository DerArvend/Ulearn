import React from 'react';
import { storiesOf } from '@storybook/react';
import NavigationContent from './NavigationContent';
import { SLIDETYPE } from '../../../../../consts/general';
import StoryRouter from 'storybook-react-router';



storiesOf('ModuleNavigation', module)
	.addDecorator(StoryRouter())
	.add('Навигация по модулю', () => (
		<NavigationContent items={getModuleNav()}/>
	));

function getModuleNav () {
	return [
		{
			url: 'https://ulearn.me/Course/BasicProgramming/Kratkaya_spravka_pered_nachalom_69a2e121-e58f-4cd0-8221-7affb7dc796e',
			type: SLIDETYPE.lesson,
			title: 'Краткая справка перед началом',
		},
		{
			url: 'https://ulearn.me/Course/BasicProgramming/Hello_world_70597ba7-f436-4301-816d-17dc34551bdb',
			type: SLIDETYPE.lesson,
			title: 'Hello world',
		},
		{
			url: 'https://ulearn.me/Course/BasicProgramming/Terminologiya_7e91aee8-0312-4298-b0e2-5d84f81bcd27',
			type: SLIDETYPE.quiz,
			title: 'Терминология',
		},
		{
			url: 'https://ulearn.me/Course/BasicProgramming/Pervyy_shag_90bcb61e-57f0-4baa-8bc9-10c9cfd27f58',
			type: SLIDETYPE.exercise,
			title: 'Первый шаг',
		},
		{
			url: 'https://ulearn.me/Course/BasicProgramming/Istochniki_informatsii_a36a9029-b83f-436e-a2b9-81e519d9a32d',
			type: SLIDETYPE.lesson,
			title: 'Источники информации',
		},
		{
			url: 'https://ulearn.me/Course/BasicProgramming/Chislovye_tipy_dannykh_8e4d3d2e-cb33-4b93-a4ea-f75dde615cf8',
			type: SLIDETYPE.lesson,
			title: 'Числовые типы данных',
		}, {
			url: 'https://ulearn.me/Course/BasicProgramming/Nevernyy_tip_dannykh_38fd7db3-e4e5-4ec1-acbd-30f3a55a70f4',
			type: SLIDETYPE.exercise,
			title: 'Неверный тип данных',
		},
		{
			url: 'https://ulearn.me/Course/BasicProgramming/Oshibki_preobrazovaniya_tipov_04d61c06-8a32-41a6-a47e-2ae2d095def3',
			type: SLIDETYPE.exercise,
			title: 'Ошибки преобразования типов',
		},
		{
			url: 'https://ulearn.me/Course/BasicProgramming/Bitkoiny_v_massy__2b6e076c-4617-4322-9bde-c44a1d4f394f',
			type: SLIDETYPE.exercise,
			title: 'Биткоины в массы!',
		},
		{
			url: 'https://ulearn.me/Course/BasicProgramming/Stroki_486be963-fbda-4c05-b56a-c226a39c3b78',
			type: SLIDETYPE.lesson,
			title: 'Строки',
		},
		{
			url: 'https://ulearn.me/Course/BasicProgramming/Preobrazovanie_stroki_v_chislo_ace76228-b605-43b8-ac8d-4c85033ac7df',
			type: SLIDETYPE.exercise,
			title: 'Преобразование строки в число',
		},
		{
			url: 'https://ulearn.me/Course/BasicProgramming/Arifmeticheskie_operatsii_i_var_03dba3bd-60d3-417f-a44c-d5319da3245f',
			type: SLIDETYPE.lesson,
			title: 'Арифметические операции и var',
		},
		{
			url: 'https://ulearn.me/Course/BasicProgramming/Ispol_zovanie_var_956a2160-ceb6-4485-8a94-c39dfe364f48',
			type: SLIDETYPE.exercise,
			title: 'Использование var',
		},
		{
			url: 'https://ulearn.me/Course/BasicProgramming/Metody_45b1b595-0489-4713-bc82-22e188fd8472',
			type: SLIDETYPE.lesson,
			title: 'Методы',
		},
		{
			url: 'https://ulearn.me/Course/BasicProgramming/Dobryy_rabotodatel__f6559650-b3af-4e5e-be84-941fb21bc2ac',
			type: SLIDETYPE.exercise,
			title: 'Добрый работодатель',
		},
		{
			url: 'https://ulearn.me/Course/BasicProgramming/Glavnyy_vopros_Vselennoy_d5b59540-410f-4f6d-8f04-b5613e264bd5',
			type: SLIDETYPE.exercise,
			title: 'Главный вопрос Вселенной',
		},
		{
			url: 'https://ulearn.me/Course/BasicProgramming/Razyskivayutsya_metody__d8578c7f-d7e2-4ce5-a3be-e6e32d9ac63e',
			type: SLIDETYPE.exercise,
			title: 'Разыскиваются методы!',
		},
		{
			url: 'https://ulearn.me/Course/BasicProgramming/Peremennye_3d36666d-c973-4c0a-938a-ac71ce3847fe',
			type: SLIDETYPE.lesson,
			title: 'Переменные',
		},
		{
			url: 'https://ulearn.me/Course/BasicProgramming/Oblasti_vidimosti_a7a73125-2434-4b5d-9f31-4ef687fc8bcc',
			type: SLIDETYPE.quiz,
			title: 'Области видимости',
		},
		{
			url: 'https://ulearn.me/Course/BasicProgramming/Zadachi_na_seminar_aef25d82-818b-4a50-a2f0-bba842eeeedc',
			type: SLIDETYPE.lesson,
			title: 'Задачи на семинар',
		},
	].map(item => {
		if (Math.random() > 0.5) {
			item.complete = true;
		}

		if (item.complete && item.type !== SLIDETYPE.lesson) {
			item.progress = Math.random();
		}

		return item;
	});
}
