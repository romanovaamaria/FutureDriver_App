document.addEventListener('DOMContentLoaded', function () {
    // Існуючі змінні
    const cards = document.querySelectorAll('.card');
    let flippedCards = new Set();
    let selectedCard = null;
    const modal = new bootstrap.Modal(document.getElementById('ratingModal'));

    // Нові змінні для компактних карток
    const compactCards = document.querySelectorAll('.compact-card');
    let currentFullCard = null;
    const fullCardModal = document.getElementById('fullCardModal') ?
        new bootstrap.Modal(document.getElementById('fullCardModal')) : null;

    // Існуючий код для повних карток (зберігаємо без змін)
    cards.forEach(function (card) {
        card.addEventListener('click', function (e) {
            if (e.target.classList.contains('delete-btn') || e.target.closest('.delete-btn')) {
                return;
            }

            card.classList.toggle('flip');
            const questionId = card.dataset.questionId;

            if (flippedCards.has(questionId)) {
                selectedCard = card;
                modal.show();
            } else {
                flippedCards.add(questionId);
            }
        });

        card.setAttribute('draggable', 'true');
        card.addEventListener('dragstart', handleDragStart);
    });

    //  Обробка компактних карток
    compactCards.forEach(function (card) {
        card.addEventListener('click', function (e) {
            // Перевіряємо чи це не кнопка видалення
            if (e.target.classList.contains('delete-btn') || e.target.closest('.delete-btn')) {
                return;
            }

            const questionId = card.dataset.questionId;
            selectedCard = card;

            // Завантажуємо повну картку
            if (fullCardModal) {
                loadFullCard(questionId);
            }
        });

        // Drag and drop для компактних карток
        card.setAttribute('draggable', 'true');
        card.addEventListener('dragstart', handleDragStart);
    });

    //  Завантаження повної картки
    function loadFullCard(questionId) {
        if (!fullCardModal) return;

        const modalContent = document.getElementById('fullCardContent');
        const flipBtn = document.getElementById('flipCardBtn');

        if (!modalContent || !flipBtn) return;

        // Показуємо loader
        modalContent.innerHTML = `
            <div class="modal-loader d-flex justify-content-center align-items-center" style="height: 200px;">
                <div class="spinner-border" role="status">
                    <span class="visually-hidden">Завантаження...</span>
                </div>
            </div>
        `;

        // Скидаємо стан кнопки
        flipBtn.textContent = 'Показати відповідь';
        flipBtn.onclick = flipFullCard;

        fullCardModal.show();

        // Завантажуємо повну картку з сервера
        fetch(`/Card/GetFullCard?id=${questionId}`)
            .then(response => response.text())
            .then(html => {
                modalContent.innerHTML = html;
                currentFullCard = modalContent.querySelector('.full-card');
            })
            .catch(error => {
                console.error('Error:', error);
                modalContent.innerHTML = '<p class="text-danger">Помилка завантаження картки</p>';
            });
    }

    //  Перевороту повної картки в модалі
    function flipFullCard() {
        if (!currentFullCard) return;

        const flipBtn = document.getElementById('flipCardBtn');
        const questionId = currentFullCard.dataset.questionId;

        if (flippedCards.has(questionId)) {
            // Картка вже перевернута, показуємо оцінку
            fullCardModal.hide();
            modal.show();
        } else {
            // Перевертаємо картку
            currentFullCard.classList.add('flip');
            flippedCards.add(questionId);
            flipBtn.textContent = 'Оцінити відповідь';
            flipBtn.onclick = () => {
                fullCardModal.hide();
                modal.show();
            };
        }
    }

    //  Швидка оцінка без модального вікна
    window.rateAndClose = function (rating) {
        if (!currentFullCard) return;
        const questionId = currentFullCard.dataset.questionId;
        submitRating(questionId, rating);
        fullCardModal.hide();
    };

    //  Відправка оцінки
    function submitRating(questionId, rating) {
        fetch('/Card/RateAnswer', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({ QuestionId: parseInt(questionId), Quality: rating })
        })
            .then(response => {
                if (response.ok) {
                    modal.hide();
                    // Видаляємо картку після оцінки (працює як для повних, так і компактних)
                    if (selectedCard) {
                        selectedCard.remove();
                    }
                    flippedCards.delete(questionId);
                } else {
                    alert('Помилка при збереженні оцінки');
                }
            })
            .catch(error => {
                console.error('Error:', error);
                alert('Помилка при збереженні оцінки');
            });
    }

    //  rate buttons
    document.querySelectorAll('.rate-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const rating = parseInt(this.dataset.rating);
            let questionId;

            // Визначаємо ID питання в залежності від типу картки
            if (currentFullCard) {
                questionId = currentFullCard.dataset.questionId;
            } else if (selectedCard) {
                questionId = selectedCard.dataset.questionId;
            } else {
                return;
            }

            submitRating(questionId, rating);
        });
    });

    //handleDragStart 
    function handleDragStart(e) {
        e.dataTransfer.setData('text/plain', e.target.dataset.questionId);
    }

    // drag and drop 
    document.querySelectorAll('.drop-zone').forEach(zone => {
        zone.addEventListener('dragover', e => {
            e.preventDefault();
            zone.classList.add('drag-over');
        });

        zone.addEventListener('dragleave', e => {
            zone.classList.remove('drag-over');
        });

        zone.addEventListener('drop', e => {
            e.preventDefault();
            zone.classList.remove('drag-over');

            const questionId = e.dataTransfer.getData('text/plain');
            const targetDate = zone.dataset.date;

            let formattedDate = targetDate;
            if (targetDate === 'new') {
                formattedDate = null;
            } else if (targetDate === 'overdue') {
                formattedDate = new Date().toISOString().split('T')[0];
            }

            fetch('/Card/MoveCard', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify({ Id: parseInt(questionId), NewDate: formattedDate })
            })
                .then(response => {
                    if (response.ok) {
                        // Переміщуємо картку візуально (працює як для .card, так і .compact-card)
                        const card = document.querySelector(`[data-question-id="${questionId}"]`);
                        if (card) {
                            const cardContainer = zone.querySelector('.card-container');
                            if (cardContainer) {
                                cardContainer.appendChild(card);
                            } else {
                                const newContainer = document.createElement('div');
                                newContainer.className = 'card-container';
                                newContainer.appendChild(card);

                                const emptyMessage = zone.querySelector('.text-muted');
                                if (emptyMessage) {
                                    emptyMessage.remove();
                                }

                                zone.appendChild(newContainer);
                            }
                        }
                    } else {
                        alert('Помилка при переміщенні картки');
                    }
                })
                .catch(error => {
                    console.error('Error:', error);
                    alert('Помилка при переміщенні картки');
                });
        });
    });
});

// deleteQuestion (без змін)
function deleteQuestion(questionId) {
    if (confirm('Ви впевнені, що хочете видалити це питання?')) {
        const form = document.createElement('form');
        form.method = 'POST';
        form.action = '/Card/DeleteQuestion';

        const input = document.createElement('input');
        input.type = 'hidden';
        input.name = 'questionId';
        input.value = questionId;
        form.appendChild(input);

        const token = document.createElement('input');
        token.type = 'hidden';
        token.name = '__RequestVerificationToken';
        token.value = document.querySelector('input[name="__RequestVerificationToken"]').value;
        form.appendChild(token);

        document.body.appendChild(form);
        form.submit();
    }
}
