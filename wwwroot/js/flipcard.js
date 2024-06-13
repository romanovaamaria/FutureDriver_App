document.addEventListener('DOMContentLoaded', function () {
    var cards = document.querySelectorAll('.card');
    cards.forEach(function (card) {
        card.addEventListener('click', function () {
            card.classList.toggle('flip');
        });
    });
});

function deleteQuestion(questionId) {
    if (confirm('Ви впевнені, що хочете видалити це питання?')) {
        // Використовуємо звичайний (синхронний) POST-запит
        var form = document.createElement('form');
        form.method = 'POST';
        form.action = '/Card/DeleteQuestion/' + questionId; // Додаємо ID питання до URL

        // Додаємо поле з ID питання для передачі на сервер
        var input = document.createElement('input');
        input.type = 'hidden';
        input.name = 'questionId';
        input.value = questionId;
        form.appendChild(input);

        // Додаємо форму на сторінку і відправляємо запит
        document.body.appendChild(form);
        form.submit();
    }
}