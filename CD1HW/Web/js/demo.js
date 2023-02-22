window.onload = new (function () {
    //$("#addr").val("서울시 송파구 법원로 테스트 주소지");
    update_scanner_view();
})();

function sleep(millsec) {
    //return new Promise((resolve) => setTimeout(resolve, millsec));
}

function pause(numberMillis) {
    var now = new Date();
    var exitTime = now.getTime() + numberMillis;

    while (true) {
        now = new Date();
        if (now.getTime() > exitTime) return;
    }
}

// web socket를 통한 화면갱신
function update_scanner_view() {
    const url = "ws://127.0.0.1:5120/ws";
    const ws = new WebSocket(url);

    ws.onopen = function () {
        console.log("ws connected");
        ws.send("st");
    };
    ws.onmessage = function (message) {
        data = JSON.parse(message.data);
        if (data.imgBase64Str != null && data.imgBase64Str != "") {
            let imgBase64 = "data:image/jpeg;base64," + data.imgBase64Str;
            document
                .getElementById("card_scanner_view")
                .setAttribute("src", imgBase64);
        }

        if (data.name_img != null && data.name_img != "") {
            let name_img = "data:image/jpeg;base64," + data.name_img;
            document.getElementById("name_img").setAttribute("src", name_img);
        } else {
            document
                .getElementById("name_img")
                .setAttribute("src", "./img/temp/name_temp.jpg");
        }

        if (data.face_img != null && data.face_img != "") {
            let face_img = "data:image/jpeg;base64," + data.face_img;
            document.getElementById("face_img").setAttribute("src", face_img);
        } else {
            document
                .getElementById("face_img")
                .setAttribute("src", "./img/temp/face_temp.jpg");
        }

        if (data.regnum_img != null && data.regnum_img != "") {
            let regnum_img = "data:image/jpeg;base64," + data.regnum_img;
            document
                .getElementById("regnum_img")
                .setAttribute("src", regnum_img);
        } else {
            document
                .getElementById("regnum_img")
                .setAttribute("src", "./img/temp/regnum_temp.jpg");
        }

        if (data.masking_img != null && data.masking_img != "") {
            let masking_img = "data:image/jpeg;base64," + data.masking_img;
            document
                .getElementById("masking_img_modal")
                .setAttribute("src", masking_img);
        }

        // 입력 form은 value에 변화가 있을 때만 update
        const nameForm = document.getElementById("name");
        if (data.name != null && data.name != nameForm.getAttribute("value")) {
            nameForm.setAttribute("value", data.name);
            nameForm.value = data.name;
        }
        const birthForm = document.getElementById("birth");
        if (
            data.birth != null &&
            data.birth != birthForm.getAttribute("value")
        ) {
            birthForm.setAttribute("value", data.birth);
            birthForm.value = data.birth;
        }
        const regnumForm = document.getElementById("regnum");
        if (
            data.regnum != null &&
            data.regnum != regnumForm.getAttribute("value")
        ) {
            regnumForm.setAttribute("value", data.regnum);
            regnumForm.value = data.regnum;
        }
        //const addrForm = document.getElementById("addr");
        //if (data.addr != null && data.addr != addrForm.getAttribute("value")) {
        //    addrForm.setAttribute("value", data.addr);
        //    addrForm.value = data.addr;
        //}

        // 서명, 지문 이미지
        if (data.sign_img != null && data.sign_img != "") {
            let sign_img = "data:image/jpeg;base64," + data.sign_img;
            document.getElementById("sign_img").setAttribute("src", sign_img);
            document
                .getElementById("sign_img_modal")
                .setAttribute("src", sign_img);
        } else {
            document
                .getElementById("sign_img")
                .setAttribute("src", "./img/temp/sign_temp.jpg");
            document
                .getElementById("sign_img_modal")
                .setAttribute("src", "./img/temp/sign_temp.jpg");
        }
        if (data.finger_img != null && data.finger_img != "") {
            let finger_img = "data:image/jpeg;base64," + data.finger_img;
            document
                .getElementById("finger_img")
                .setAttribute("src", finger_img);
            document
                .getElementById("finger_img_modal")
                .setAttribute("src", finger_img);
        } else {
            document
                .getElementById("finger_img")
                .setAttribute("src", "./img/temp/finger_temp.jpg");
            document
                .getElementById("finger_img_modal")
                .setAttribute("src", "./img/temp/finger_temp.jpg");
        }

        //성별
        if (data.sex != null && data.sex != "") {
            if (data.sex == "1") {
                document.getElementById("sex_m").setAttribute("checked", true);
                document
                    .getElementById("sex_f")
                    .removeAttribute("checked", true);
            } else if (data.sex == "2") {
                document
                    .getElementById("sex_m")
                    .removeAttribute("checked", true);
                document.getElementById("sex_f").setAttribute("checked", true);
            }
        } else {
            document.getElementById("sex_m").removeAttribute("checked", true);
            document.getElementById("sex_f").removeAttribute("checked", true);
        }

        pause(30);
        ws.send("nf");
    };
}

function call_scanner() {
    const url = "http://localhost:5120/call_padandfinger";
    let form = {
        name: $("#name").val(),
        birth: $("#birth").val(),
        regnum: $("#regnum").val(),
        addr: $("#addr").val(),
    };
    console.log(JSON.stringify(form));
    const formData = JSON.stringify(form);
    $.ajax({
        url: url,
        type: "POST",
        processData: false,
        contentType: "application/json",
        data: formData,
        dataType: "json",
        success: function (data) {
            console.log(data);
            if (data == 1) {
                pause(2000);
                call_scanner();
            }
        },
        error: function (data) {},
    });
}
function cancel_scanner() {
    const url = "http://localhost:5120/cancel_padandfinger";
    $.ajax({
        url: url,
        type: "GET",
        processData: false,
        contentType: "application/json",
        dataType: "json",
        success: function (data) {
            console.log(data);
            if (data == 1) {
                call_scanner();
            }
        },
        error: function (data) {},
    });
}

function query_addr_4_nec() {
    const url = "http://localhost:5120/query_addr_4_nec";
    let form = {
        name: $("#name").val(),
        birth: $("#birth").val(),
        regnum: $("#regnum").val(),
    };
    console.log(JSON.stringify(form));
    const formData = JSON.stringify(form);
    $.ajax({
        url: url,
        type: "POST",
        processData: false,
        contentType: "application/json",
        data: formData,
        dataType: "json",
        success: function (data) {
            console.log(data);
            if (data.length == 1) {
                document.getElementById("name").value = data[0].name;
                document.getElementById("addr").value = data[0].address;
                call_scanner();
            } else if (data.length > 1) {
                $("#addrTableBody").html(
                    "<tr><th>성명</th><th>개인식별번호</th><th>주소</th><th>선택</th></tr>"
                );
                data.forEach((item, index) => {
                    let ch =
                        "<tr><td>" +
                        item.name +
                        "</td><td>" +
                        item.regnum +
                        "</td><td>" +
                        item.address +
                        '</td><td><button type="button" class="btn btn-primary" onClick="setAddr(' +
                        index +
                        ')">선택</button></tr>';
                    console.log(ch);
                    $("#addrTableBody").append(ch);
                });
                $("#selAddrModal").modal("show");
            } else {
                //document.getElementById("addr").value ="주소지를 찾을 수 없습니다";
                $("#addrNotFoundModal").modal("show");
            }
        },
        error: function (data) {
            $("#addrNotFoundModal").modal("show");
        },
    });
}

function setAddr(idx) {
    let table = document.getElementById("addrTable");
    let row = table.rows[idx + 1];
    let cellName = row.cells[0];
    let cellAddr = row.cells[2];
    $("#selAddrModal").modal("hide");
    document.getElementById("addr").value = cellAddr.innerText;
    document.getElementById("name").value = cellName.innerText;
    call_scanner();
}

function manual_scan() {
    const url = "http://localhost:5120/manual_scan";
    $.ajax({
        url: url,
        type: "GET",
        processData: false,
        contentType: "application/json",
        dataType: "json",
        success: function (data) {
            console.log(data);
            document.getElementById("manual_time").innerText =
                "경과시간 : " + data / 10000000 + " 초";
        },
        error: function (data) {},
    });
}

function ocr_reset() {
    const url = "http://localhost:5120/ocr_reset";
    document.getElementById("name").value = "";
    document.getElementById("birth").value = "";
    document.getElementById("regnum").value = "";
    document.getElementById("addr").value = "";
    document.getElementById("manual_time").innerText = "";
    $.ajax({
        url: url,
        type: "GET",
        processData: false,
        success: function (data) {
            console.log(data);
        },
        error: function (data) {},
    });
}

function call_speaker() {
    const url = "http://localhost:2907/code1hw/";
    let form = {
        msg_type: "call_hw",
        call_hw_ids: ["SPEAKER"],
        hardware_parm: {
            sound_to_play: $("#addr").val(),
        },
    };
    console.log(JSON.stringify(form));
    const formData = JSON.stringify(form);
    $.ajax({
        url: url,
        type: "POST",
        processData: false,
        contentType: false,
        data: formData,
        dataType: "json",
        success: function (data) {},
        error: function (data) {},
    });
}
