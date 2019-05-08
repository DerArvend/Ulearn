export default function colorHash(str) {
	let sum = 0;

	for (let i in str) {
		sum += str.charCodeAt(i);
	}

	let hue = Math.abs(sum) % 360;

	return `hsl(${hue}, 64%, 75%)`;
}
