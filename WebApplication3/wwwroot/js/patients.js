// Этот скрипт управляет таблицей пациентов, модальными окнами, масками ввода,
// поиском/сортировкой, загрузкой/сохранением через AJAX.

$(function () {
    // Текущая сортировка
    var sortField = "FullName";
    var sortDir = "asc";

    // Инициализация диалогов jQuery UI
    $("#patientModal").dialog({
        autoOpen: false,
        modal: true,
        width: 500,
        buttons: {
            "Сохранить": function () {
                savePatient();
            },
            "Отмена": function () {
                $(this).dialog("close");
            }
        }
    });

    $("#visitModal").dialog({
        autoOpen: false,
        modal: true,
        width: 600,
        buttons: {
            "Сохранить визит": function () {
                saveVisit();
            },
            "Отмена": function () {
                $(this).dialog("close");
            }
        }
    });

    // Маски ввода
    // Телефонная маска
    $("#phone").mask("+7 (000) 000-00-00");
    // Дата рождения и дата визита — простая маска ГГГГ-ММ-ДД
    $("#birthDate, #visitDate").mask("0000-00-00");

    // Автокомплит по МКБ-10
    $("#icdLookup").autocomplete({
        minLength: 2,
        source: function (request, response) {
            $.getJSON("/Visits/IcdLookup", { term: request.term }, function (data) {
                response($.map(data, function (item) {
                    return {
                        label: item.code + " — " + item.name,
                        value: item.code,
                        id: item.id
                    };
                }));
            });
        },
        select: function (event, ui) {
            $("#icdCodeText").val(ui.item.value);
            $("#icdCodeId").val(ui.item.id);
        }
    });

    // Кнопки
    $("#btnAddPatient").on("click", function () {
        // Очистка формы
        $("#patientForm")[0].reset();
        $("#patientId").val("");
        $("#patientModal").dialog("option", "title", "Добавить пациента").dialog("open");
    });

    // Поиск
    $("#searchQuery").on("input", function () {
        loadPatients();
    });

    // Сортировка — клики по заголовкам
    $("#patientsTable th[data-sort]").on("click", function () {
        var field = $(this).data("sort");
        if (sortField === field) {
            sortDir = (sortDir === "asc") ? "desc" : "asc";
        } else {
            sortField = field;
            sortDir = "asc";
        }
        loadPatients();
    });

    // Первичная загрузка
    loadPatients();

    // Загрузка таблицы пациентов
    function loadPatients() {
        var query = $("#searchQuery").val();
        $.getJSON("/Patients/List", { query: query, sort: sortField, dir: sortDir }, function (data) {
            var tbody = $("#patientsTable tbody");
            tbody.empty();
            $.each(data, function (i, item) {
                var tr = $("<tr>");
                tr.append($("<td>").text(item.fullName));
                tr.append($("<td>").text(item.birthDate));
                tr.append($("<td>").text(item.phone || ""));
                tr.append($("<td>").text(item.notes || ""));

                var actions = $("<td>").addClass("text-end");
                var btnEdit = $("<button>").addClass("btn btn-sm btn-secondary me-1").text("Редактировать").on("click", function () {
                    editPatient(item.id);
                });
                var btnVisits = $("<button>").addClass("btn btn-sm btn-info me-1").text("Посещения").on("click", function () {
                    openVisits(item.id, item.fullName);
                });
                var btnExport = $("<button>").addClass("btn btn-sm btn-outline-primary me-1").text("Экспорт XML").on("click", function () {
                    exportXml(item.id);
                });
                var btnDelete = $("<button>").addClass("btn btn-sm btn-danger").text("Удалить").on("click", function () {
                    deletePatient(item.id);
                });

                actions.append(btnEdit, btnVisits, btnExport, btnDelete);
                tr.append(actions);
                tbody.append(tr);
            });
        });
    }

    // Редактирование пациента — загрузка данных и открытие модалки
    function editPatient(id) {
        $.getJSON("/Patients/Get", { id: id }, function (p) {
            $("#patientId").val(p.id);
            $("#fullName").val(p.fullName);
            $("#birthDate").val(p.birthDate);
            $("#phone").val(p.phone || "");
            $("#notes").val(p.notes || "");
            $("#patientModal").dialog("option", "title", "Редактировать пациента").dialog("open");
        });
    }

    // Сохранение пациента — если есть id, обновляем, иначе создаём
    function savePatient() {
        // Простейшая фронт-валидация длины/обязательности
        var fullName = $("#fullName").val();
        var birthDate = $("#birthDate").val();
        if (!fullName || !birthDate) {
            alert("Заполните обязательные поля: ФИО и дата рождения");
            return;
        }
        if (fullName.length > 200) {
            alert("Слишком длинное ФИО");
            return;
        }

        var id = $("#patientId").val();
        var payload = {
            Id: id,
            FullName: fullName,
            BirthDate: birthDate,
            Phone: $("#phone").val(),
            Notes: $("#notes").val()
        };

        var url = id ? "/Patients/Update" : "/Patients/Create";

        $.ajax({
            url: url,
            method: "POST",
            data: payload,
            headers: { "RequestVerificationToken": getCsrfToken() }
        })
            .done(function () {
                $("#patientModal").dialog("close");
                loadPatients();
            })
            .fail(function (xhr) {
                alert("Ошибка сохранения: " + (xhr.responseJSON?.error || xhr.statusText));
            });
    }

    // Удаление пациента
    function deletePatient(id) {
        if (!confirm("Удалить пациента? Это действие необратимо.")) return;

        $.ajax({
            url: "/Patients/Delete",
            method: "POST",
            data: { id: id },
            headers: { "RequestVerificationToken": getCsrfToken() }
        })
            .done(function () {
                loadPatients();
            })
            .fail(function (xhr) {
                alert("Ошибка удаления: " + (xhr.responseJSON?.error || xhr.statusText));
            });
    }

    // Окно посещений
    function openVisits(patientId, fullName) {
        $("#visitPatientId").val(patientId);
        $("#visitForm")[0].reset();
        $("#visitModal").dialog("option", "title", "Посещения: " + fullName).dialog("open");
        // Можно дополнительно отрисовать таблицу посещений пациента
        // Здесь показываем минимум — в реальности вставьте таблицу или секцию.
        // Для наглядности — загружаем посещения и выводим в консоль.
        $.getJSON("/Patients/Visits", { id: patientId }, function (list) {
            console.log("Visits:", list);
        });
    }

    // Сохранение визита
    function saveVisit() {
        var patientId = $("#visitPatientId").val();
        var visitDate = $("#visitDate").val();

        if (!visitDate) {
            alert("Дата визита обязательна");
            return;
        }

        var data = {
            patientId: patientId,
            visitDate: visitDate,
            icdCodeText: $("#icdCodeText").val(),
            icdCodeId: $("#icdCodeId").val(),
            description: $("#description").val()
        };

        $.ajax({
            url: "/Patients/AddVisit",
            method: "POST",
            data: data,
            headers: { "RequestVerificationToken": getCsrfToken() }
        })
            .done(function () {
                $("#visitModal").dialog("close");
            })
            .fail(function (xhr) {
                alert("Ошибка сохранения визита: " + (xhr.responseJSON?.error || xhr.statusText));
            });
    }

    // Экспорт XML
    function exportXml(id) {
        window.location.href = "/Patients/ExportXml?id=" + encodeURIComponent(id);
    }

    // Получение CSRF (AntiForgeryToken) из cookie ASP.NET Core
    function getCsrfToken() {
        var name = "__RequestVerificationToken=";
        var cookies = document.cookie.split("; ");
        for (var i = 0; i < cookies.length; i++) {
            if (cookies[i].indexOf(name) === 0) {
                return cookies[i].substring(name.length);
            }
        }
        return "";
    }
});
